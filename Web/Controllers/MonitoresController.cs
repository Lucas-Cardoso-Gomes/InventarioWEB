using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Web.Models;
using Web.Services;
using Monitor = Web.Models.Monitor;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using System.IO;
using System.Threading.Tasks;
using System.Data;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin,Coordenador,Colaborador,Diretoria")]
    public class MonitoresController : Controller
    {
        private readonly IDatabaseService _databaseService;
        private readonly ILogger<MonitoresController> _logger;

        public MonitoresController(IDatabaseService databaseService, ILogger<MonitoresController> logger, PersistentLogService persistentLogService)
        {
            _databaseService = databaseService;
            _logger = logger;
        }

        public IActionResult Index(List<string> currentMarcas, List<string> currentTamanhos, List<string> currentModelos)
        {
            var viewModel = new MonitorIndexViewModel
            {
                CurrentMarcas = currentMarcas,
                CurrentTamanhos = currentTamanhos,
                CurrentModelos = currentModelos,
                Monitores = new List<Monitor>()
            };

            try
            {
                using (var connection = _databaseService.CreateConnection())
                {
                    connection.Open();

                    viewModel.Marcas = GetDistinctMonitorValues(connection, "Marca");
                    viewModel.Tamanhos = GetDistinctMonitorValues(connection, "Tamanho");
                    viewModel.Modelos = GetDistinctMonitorValues(connection, "Modelo");

                    var whereClauses = new List<string>();
                    var parameters = new Dictionary<string, object>();
                    var userCpf = User.FindFirstValue("ColaboradorCPF");

                    if (User.IsInRole("Colaborador") && !User.IsInRole("Admin") && !User.IsInRole("Diretoria"))
                    {
                        whereClauses.Add("m.ColaboradorCPF = @UserCpf");
                        parameters.Add("@UserCpf", (object)userCpf ?? DBNull.Value);
                    }
                    else if (User.IsInRole("Coordenador") && !User.IsInRole("Admin") && !User.IsInRole("Diretoria"))
                    {
                        whereClauses.Add("(c.CoordenadorCPF = @UserCpf OR m.ColaboradorCPF = @UserCpf)");
                        parameters.Add("@UserCpf", (object)userCpf ?? DBNull.Value);
                    }

                    Action<string, List<string>> addInClause = (columnName, values) =>
                    {
                        if (values != null && values.Any())
                        {
                            var paramNames = new List<string>();
                            for (int i = 0; i < values.Count; i++)
                            {
                                var paramName = $"@{(columnName.Split('.').Last()).ToLower()}{i}";
                                paramNames.Add(paramName);
                                parameters.Add(paramName, values[i]);
                            }
                            whereClauses.Add($"{columnName} IN ({string.Join(", ", paramNames)})");
                        }
                    };

                    addInClause("m.Marca", currentMarcas);
                    addInClause("m.Tamanho", currentTamanhos);
                    addInClause("m.Modelo", currentModelos);

                    string whereSql = whereClauses.Any() ? $"WHERE {string.Join(" AND ", whereClauses)}" : "";

                    string sql = $"SELECT m.*, c.Nome as ColaboradorNome FROM Monitores m LEFT JOIN Colaboradores c ON m.ColaboradorCPF = c.CPF {whereSql}";

                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = sql;
                        foreach (var p in parameters)
                        {
                            var param = cmd.CreateParameter();
                            param.ParameterName = p.Key;
                            param.Value = p.Value;
                            cmd.Parameters.Add(param);
                        }

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                viewModel.Monitores.Add(new Monitor
                                {
                                    PartNumber = reader["PartNumber"].ToString(),
                                    ColaboradorCPF = reader["ColaboradorCPF"] != DBNull.Value ? reader["ColaboradorCPF"].ToString() : null,
                                    ColaboradorNome = reader["ColaboradorNome"] != DBNull.Value ? reader["ColaboradorNome"].ToString() : null,
                                    Marca = reader["Marca"].ToString(),
                                    Modelo = reader["Modelo"].ToString(),
                                    Tamanho = reader["Tamanho"].ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter a lista de monitores.");
            }
            return View(viewModel);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Importar(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Nenhum arquivo selecionado.";
                return RedirectToAction(nameof(Index));
            }

            var monitores = new List<Monitor>();
            try
            {
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    using (var package = new ExcelPackage(stream))
                    {
                        ExcelWorksheet worksheet = package.Workbook.Worksheets.FirstOrDefault();
                        if (worksheet == null)
                        {
                            TempData["ErrorMessage"] = "A planilha do Excel está vazia ou não foi encontrada.";
                            return RedirectToAction(nameof(Index));
                        }

                        int rowCount = worksheet.Dimension.Rows;
                        for (int row = 2; row <= rowCount; row++)
                        {
                            var sanitizedCpf = SanitizeCpf(worksheet.Cells[row, 2].Value?.ToString().Trim());
                            var monitor = new Monitor
                            {
                                PartNumber = worksheet.Cells[row, 1].Value?.ToString().Trim(),
                                ColaboradorCPF = string.IsNullOrEmpty(sanitizedCpf) ? null : sanitizedCpf,
                                Marca = worksheet.Cells[row, 3].Value?.ToString().Trim(),
                                Modelo = worksheet.Cells[row, 4].Value?.ToString().Trim(),
                                Tamanho = worksheet.Cells[row, 5].Value?.ToString().Trim()
                            };

                            if (!string.IsNullOrWhiteSpace(monitor.PartNumber))
                            {
                                monitores.Add(monitor);
                            }
                        }
                    }
                }

                int adicionados = 0;
                int atualizados = 0;
                var invalidCpfs = new List<string>();

                using (var connection = _databaseService.CreateConnection())
                {
                    connection.Open();

                    var colaboradoresCpf = new HashSet<string>();
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "SELECT CPF FROM Colaboradores";
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                colaboradoresCpf.Add(reader.GetString(0));
                            }
                        }
                    }

                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            foreach (var monitor in monitores)
                            {
                                if (!string.IsNullOrEmpty(monitor.ColaboradorCPF) && !colaboradoresCpf.Contains(monitor.ColaboradorCPF))
                                {
                                    invalidCpfs.Add(monitor.PartNumber);
                                    monitor.ColaboradorCPF = null;
                                }

                                var existente = FindMonitorById(monitor.PartNumber, connection, transaction);

                                if (existente != null)
                                {
                                    string updateSql = @"UPDATE Monitores SET
                                                       ColaboradorCPF = @ColaboradorCPF, Marca = @Marca, Modelo = @Modelo, Tamanho = @Tamanho
                                                       WHERE PartNumber = @PartNumber";
                                    using (var cmd = connection.CreateCommand())
                                    {
                                        cmd.Transaction = transaction;
                                        cmd.CommandText = updateSql;
                                        AddMonitorParameters(cmd, monitor);
                                        cmd.ExecuteNonQuery();
                                    }
                                    atualizados++;
                                }
                                else
                                {
                                    string insertSql = @"INSERT INTO Monitores (PartNumber, ColaboradorCPF, Marca, Modelo, Tamanho)
                                                       VALUES (@PartNumber, @ColaboradorCPF, @Marca, @Modelo, @Tamanho)";
                                    using (var cmd = connection.CreateCommand())
                                    {
                                        cmd.Transaction = transaction;
                                        cmd.CommandText = insertSql;
                                        AddMonitorParameters(cmd, monitor);
                                        cmd.ExecuteNonQuery();
                                    }
                                    adicionados++;
                                }
                            }
                            transaction.Commit();
                            TempData["SuccessMessage"] = $"{adicionados} monitores adicionados e {atualizados} atualizados com sucesso.";
                            if (invalidCpfs.Any())
                            {
                                TempData["WarningMessage"] = $"Os seguintes monitores (PartNumber) foram importados, mas o CPF do colaborador não foi encontrado: {string.Join(", ", invalidCpfs)}";
                            }
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            _logger.LogError(ex, "Erro ao salvar os dados do Excel. A transação foi revertida.");
                            TempData["ErrorMessage"] = "Ocorreu um erro ao salvar os dados. Nenhuma alteração foi feita.";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao importar o arquivo Excel.");
                TempData["ErrorMessage"] = "Ocorreu um erro durante a importação do arquivo. Verifique se o formato está correto.";
            }

            return RedirectToAction(nameof(Index));
        }

        private void AddMonitorParameters(IDbCommand cmd, Monitor monitor)
        {
            var p1 = cmd.CreateParameter(); p1.ParameterName = "@PartNumber"; p1.Value = monitor.PartNumber; cmd.Parameters.Add(p1);
            var p2 = cmd.CreateParameter(); p2.ParameterName = "@ColaboradorCPF"; p2.Value = (object)monitor.ColaboradorCPF ?? DBNull.Value; cmd.Parameters.Add(p2);
            var p3 = cmd.CreateParameter(); p3.ParameterName = "@Marca"; p3.Value = (object)monitor.Marca ?? DBNull.Value; cmd.Parameters.Add(p3);
            var p4 = cmd.CreateParameter(); p4.ParameterName = "@Modelo"; p4.Value = (object)monitor.Modelo ?? DBNull.Value; cmd.Parameters.Add(p4);
            var p5 = cmd.CreateParameter(); p5.ParameterName = "@Tamanho"; p5.Value = (object)monitor.Tamanho ?? DBNull.Value; cmd.Parameters.Add(p5);
        }

        private Monitor FindMonitorById(string id, IDbConnection connection, IDbTransaction transaction)
        {
            Monitor monitor = null;
            try
            {
                string sql = "SELECT * FROM Monitores WHERE PartNumber = @PartNumber";
                using (var cmd = connection.CreateCommand())
                {
                    cmd.Transaction = transaction;
                    cmd.CommandText = sql;
                    var p = cmd.CreateParameter(); p.ParameterName = "@PartNumber"; p.Value = id; cmd.Parameters.Add(p);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            monitor = new Monitor
                            {
                                PartNumber = reader["PartNumber"].ToString(),
                                ColaboradorCPF = reader["ColaboradorCPF"] != DBNull.Value ? reader["ColaboradorCPF"].ToString() : null,
                                Marca = reader["Marca"].ToString(),
                                Modelo = reader["Modelo"].ToString(),
                                Tamanho = reader["Tamanho"].ToString()
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao encontrar monitor por ID.");
                if (transaction != null) throw;
            }
            return monitor;
        }

        private List<string> GetDistinctMonitorValues(IDbConnection connection, string columnName)
        {
            var values = new List<string>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = $"SELECT DISTINCT {columnName} FROM Monitores WHERE {columnName} IS NOT NULL ORDER BY {columnName}";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        values.Add(reader.GetString(0));
                    }
                }
            }
            return values;
        }

        // GET: Monitores/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewData["Colaboradores"] = new SelectList(GetColaboradores(), "CPF", "Nome");
            return View();
        }

        // POST: Monitores/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult Create(Monitor monitor)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (var connection = _databaseService.CreateConnection())
                    {
                        connection.Open();
                        string sql = "INSERT INTO Monitores (PartNumber, ColaboradorCPF, Marca, Modelo, Tamanho) VALUES (@PartNumber, @ColaboradorCPF, @Marca, @Modelo, @Tamanho)";
                        using (var cmd = connection.CreateCommand())
                        {
                            cmd.CommandText = sql;
                            AddMonitorParameters(cmd, monitor);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao criar monitor.");
                    ModelState.AddModelError(string.Empty, "Ocorreu um erro ao criar o monitor. Verifique se o PartNumber já existe.");
                }
            }
            ViewData["Colaboradores"] = new SelectList(GetColaboradores(), "CPF", "Nome", monitor.ColaboradorCPF);
            return View(monitor);
        }

        // GET: Monitores/Edit/5
        [Authorize(Roles = "Admin")]
        public IActionResult Edit(string id)
        {
            if (id == null) return NotFound();
            Monitor monitor = FindMonitorById(id);
            if (monitor == null) return NotFound();
            ViewData["Colaboradores"] = new SelectList(GetColaboradores(), "CPF", "Nome", monitor.ColaboradorCPF);
            return View(monitor);
        }

        // POST: Monitores/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult Edit(string id, Monitor monitor)
        {
            if (id != monitor.PartNumber) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    using (var connection = _databaseService.CreateConnection())
                    {
                        connection.Open();
                        string sql = "UPDATE Monitores SET ColaboradorCPF = @ColaboradorCPF, Marca = @Marca, Modelo = @Modelo, Tamanho = @Tamanho WHERE PartNumber = @PartNumber";
                        using (var cmd = connection.CreateCommand())
                        {
                            cmd.CommandText = sql;
                            AddMonitorParameters(cmd, monitor);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao editar monitor.");
                    ModelState.AddModelError(string.Empty, "Ocorreu um erro ao editar o monitor.");
                }
            }
            ViewData["Colaboradores"] = new SelectList(GetColaboradores(), "CPF", "Nome", monitor.ColaboradorCPF);
            return View(monitor);
        }

        // GET: Monitores/Delete/5
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(string id)
        {
            if (id == null) return NotFound();
            Monitor monitor = FindMonitorById(id);
            if (monitor == null) return NotFound();
            return View(monitor);
        }

        // POST: Monitores/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult DeleteConfirmed(string id)
        {
            try
            {
                using (var connection = _databaseService.CreateConnection())
                {
                    connection.Open();
                    string sql = "DELETE FROM Monitores WHERE PartNumber = @PartNumber";
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = sql;
                        var p = cmd.CreateParameter(); p.ParameterName = "@PartNumber"; p.Value = id; cmd.Parameters.Add(p);
                        cmd.ExecuteNonQuery();
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao excluir monitor.");
                ViewBag.ErrorMessage = "Ocorreu um erro ao excluir o monitor.";
                return View(FindMonitorById(id));
            }
        }

        private Monitor FindMonitorById(string id)
        {
            Monitor monitor = null;
            using (var connection = _databaseService.CreateConnection())
            {
                connection.Open();
                string sql = "SELECT m.*, c.Nome AS ColaboradorNome FROM Monitores m LEFT JOIN Colaboradores c ON m.ColaboradorCPF = c.CPF WHERE m.PartNumber = @PartNumber";
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = sql;
                    var p = cmd.CreateParameter(); p.ParameterName = "@PartNumber"; p.Value = id; cmd.Parameters.Add(p);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            monitor = new Monitor
                            {
                                PartNumber = reader["PartNumber"].ToString(),
                                ColaboradorCPF = reader["ColaboradorCPF"] != DBNull.Value ? reader["ColaboradorCPF"].ToString() : null,
                                ColaboradorNome = reader["ColaboradorNome"] != DBNull.Value ? reader["ColaboradorNome"].ToString() : null,
                                Marca = reader["Marca"].ToString(),
                                Modelo = reader["Modelo"].ToString(),
                                Tamanho = reader["Tamanho"].ToString()
                            };
                        }
                    }
                }
            }
            return monitor;
        }

        private List<Colaborador> GetColaboradores()
        {
            var colaboradores = new List<Colaborador>();
            using (var connection = _databaseService.CreateConnection())
            {
                connection.Open();
                string sql = "SELECT CPF, Nome FROM Colaboradores ORDER BY Nome";
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = sql;
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            colaboradores.Add(new Colaborador {
                                CPF = reader["CPF"].ToString(),
                                Nome = reader["Nome"].ToString()
                            });
                        }
                    }
                }
            }
            return colaboradores;
        }

        private string SanitizeCpf(string cpf)
        {
            if (string.IsNullOrEmpty(cpf)) return cpf;
            return new string(cpf.Where(char.IsDigit).ToArray());
        }
    }
}
