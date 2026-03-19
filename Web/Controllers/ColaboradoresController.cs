using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Web.Models;
using Web.Services;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin,Coordenador")]
    public class ColaboradoresController : Controller
    {
        private readonly IDatabaseService _databaseService;
        private readonly ILogger<ColaboradoresController> _logger;
        private readonly PersistentLogService _persistentLogService;

        public ColaboradoresController(IDatabaseService databaseService, ILogger<ColaboradoresController> logger, PersistentLogService persistentLogService)
        {
            _databaseService = databaseService;
            _logger = logger;
            _persistentLogService = persistentLogService;
        }

        private string SanitizeCpf(string cpf)
        {
            if (string.IsNullOrEmpty(cpf)) return cpf;
            return new string(cpf.Where(char.IsDigit).ToArray());
        }

        public IActionResult Index(
            string sortOrder, string searchString,
            List<string> currentFiliais, List<string> currentSetores, List<string> currentSmartphones,
            List<string> currentTelefoneFixos, List<string> currentRamais, List<string> currentCoordenadores,
            int pageNumber = 1, int pageSize = 25)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NomeSortParm"] = string.IsNullOrEmpty(sortOrder) ? "nome_desc" : "";
            ViewData["EmailSortParm"] = sortOrder == "email" ? "email_desc" : "email";
            ViewData["FilialSortParm"] = sortOrder == "filial" ? "filial_desc" : "filial";
            ViewData["SetorSortParm"] = sortOrder == "setor" ? "setor_desc" : "setor";

            var viewModel = new ColaboradorIndexViewModel
            {
                Colaboradores = new List<Colaborador>(),
                PageNumber = pageNumber,
                PageSize = pageSize,
                SearchString = searchString,
                CurrentSort = sortOrder,
                CurrentFiliais = currentFiliais ?? new List<string>(),
                CurrentSetores = currentSetores ?? new List<string>(),
                CurrentSmartphones = currentSmartphones ?? new List<string>(),
                CurrentTelefoneFixos = currentTelefoneFixos ?? new List<string>(),
                CurrentRamais = currentRamais ?? new List<string>(),
                CurrentCoordenadores = currentCoordenadores ?? new List<string>()
            };

            try
            {
                using (var connection = _databaseService.CreateConnection())
                {
                    connection.Open();

                    viewModel.Filiais = GetDistinctColaboradorValues(connection, "Filial");
                    viewModel.Setores = GetDistinctColaboradorValues(connection, "Setor");
                    viewModel.Smartphones = GetDistinctColaboradorValues(connection, "Smartphone");
                    viewModel.TelefoneFixos = GetDistinctColaboradorValues(connection, "TelefoneFixo");
                    viewModel.Ramais = GetDistinctColaboradorValues(connection, "Ramal");
                    viewModel.Coordenadores = GetCoordenadores().Select(c => c.Nome).ToList();

                    var whereClauses = new List<string>();
                    var parameters = new Dictionary<string, object>();
                    string baseSql = "FROM Colaboradores c LEFT JOIN Colaboradores co ON c.CoordenadorCPF = co.CPF";

                    if (!string.IsNullOrEmpty(searchString))
                    {
                        whereClauses.Add("(c.Nome LIKE @search OR c.CPF LIKE @search OR c.Email LIKE @search)");
                        parameters.Add("@search", $"%{searchString}%");
                    }

                    Action<string, List<string>> addInClause = (columnName, values) =>
                    {
                        if (values != null && values.Any())
                        {
                            var paramNames = new List<string>();
                            for (int i = 0; i < values.Count; i++)
                            {
                                var paramName = $"@{columnName.ToLower().Replace(".", "")}{i}";
                                paramNames.Add(paramName);
                                parameters.Add(paramName, values[i]);
                            }
                            whereClauses.Add($"{columnName} IN ({string.Join(", ", paramNames)})");
                        }
                    };

                    addInClause("c.Filial", viewModel.CurrentFiliais);
                    addInClause("c.Setor", viewModel.CurrentSetores);
                    addInClause("c.Smartphone", viewModel.CurrentSmartphones);
                    addInClause("c.TelefoneFixo", viewModel.CurrentTelefoneFixos);
                    addInClause("c.Ramal", viewModel.CurrentRamais);
                    addInClause("co.Nome", viewModel.CurrentCoordenadores);

                    string whereSql = whereClauses.Any() ? $"WHERE {string.Join(" AND ", whereClauses)}" : "";

                    string countSql = $"SELECT COUNT(c.CPF) {baseSql} {whereSql}";
                    using (var countCommand = connection.CreateCommand())
                    {
                        countCommand.CommandText = countSql;
                        foreach (var p in parameters) {
                             var param = countCommand.CreateParameter();
                             param.ParameterName = p.Key;
                             param.Value = p.Value;
                             countCommand.Parameters.Add(param);
                        }
                        var result = countCommand.ExecuteScalar();
                        viewModel.TotalCount = result != DBNull.Value ? Convert.ToInt32(result) : 0;
                    }

                    string orderBySql;
                    switch (sortOrder)
                    {
                        case "nome_desc": orderBySql = "ORDER BY c.Nome DESC"; break;
                        case "email": orderBySql = "ORDER BY c.Email"; break;
                        case "email_desc": orderBySql = "ORDER BY c.Email DESC"; break;
                        case "filial": orderBySql = "ORDER BY c.Filial"; break;
                        case "filial_desc": orderBySql = "ORDER BY c.Filial DESC"; break;
                        case "setor": orderBySql = "ORDER BY c.Setor"; break;
                        case "setor_desc": orderBySql = "ORDER BY c.Setor DESC"; break;
                        default: orderBySql = "ORDER BY c.Nome"; break;
                    }

                    string sql = $"SELECT c.*, co.Nome as CoordenadorNome {baseSql} {whereSql} {orderBySql} LIMIT @pageSize OFFSET @offset";
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = sql;
                        foreach (var p in parameters) {
                             var param = cmd.CreateParameter();
                             param.ParameterName = p.Key;
                             param.Value = p.Value;
                             cmd.Parameters.Add(param);
                        }
                        var pOffset = cmd.CreateParameter(); pOffset.ParameterName = "@offset"; pOffset.Value = (pageNumber - 1) * pageSize; cmd.Parameters.Add(pOffset);
                        var pPageSize = cmd.CreateParameter(); pPageSize.ParameterName = "@pageSize"; pPageSize.Value = pageSize; cmd.Parameters.Add(pPageSize);


                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                viewModel.Colaboradores.Add(new Colaborador
                                {
                                    CPF = reader["CPF"].ToString(),
                                    Nome = reader["Nome"].ToString(),
                                    Email = reader["Email"].ToString(),
                                    Filial = reader["Filial"].ToString(),
                                    Setor = reader["Setor"].ToString(),
                                    DataInclusao = reader["DataInclusao"] != DBNull.Value ? Convert.ToDateTime(reader["DataInclusao"]) : (DateTime?)null,
                                    DataAlteracao = reader["DataAlteracao"] != DBNull.Value ? Convert.ToDateTime(reader["DataAlteracao"]) : (DateTime?)null,
                                    CoordenadorNome = reader["CoordenadorNome"] != DBNull.Value ? reader["CoordenadorNome"].ToString() : null
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter a lista de colaboradores.");
                ViewBag.Message = "Ocorreu um erro ao obter a lista de colaboradores. Por favor, tente novamente mais tarde.";
            }

            return View(viewModel);
        }

        // GET: Colaboradores/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewBag.Coordenadores = new SelectList(GetCoordenadores(), "CPF", "Nome");
            return View();
        }

        // POST: Colaboradores/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Colaborador colaborador)
        {
            colaborador.CPF = SanitizeCpf(colaborador.CPF);
            colaborador.CoordenadorCPF = SanitizeCpf(colaborador.CoordenadorCPF);

            if (ModelState.IsValid)
            {
                try
                {
                    using (var connection = _databaseService.CreateConnection())
                    {
                        connection.Open();
                        string sql = @"INSERT INTO Colaboradores (CPF, Nome, Email, SenhaEmail, Teams, SenhaTeams, EDespacho, SenhaEDespacho, Genius, SenhaGenius, Ibrooker, SenhaIbrooker, Adicional, SenhaAdicional, Filial, Setor, Smartphone, TelefoneFixo, Ramal, Alarme, Videoporteiro, Obs, DataInclusao, CoordenadorCPF) 
                                       VALUES (@CPF, @Nome, @Email, @SenhaEmail, @Teams, @SenhaTeams, @EDespacho, @SenhaEDespacho, @Genius, @SenhaGenius, @Ibrooker, @SenhaIbrooker, @Adicional, @SenhaAdicional, @Filial, @Setor, @Smartphone, @TelefoneFixo, @Ramal, @Alarme, @Videoporteiro, @Obs, @DataInclusao, @CoordenadorCPF)";
                        using (var cmd = connection.CreateCommand())
                        {
                            cmd.CommandText = sql;
                            AddColaboradorParameters(cmd, colaborador);
                            var pDate = cmd.CreateParameter(); pDate.ParameterName = "@DataInclusao"; pDate.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); cmd.Parameters.Add(pDate);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    await _persistentLogService.LogChangeAsync("Colaborador", "Create", User.Identity.Name, null, colaborador);

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao criar colaborador.");
                    ModelState.AddModelError(string.Empty, "Ocorreu um erro ao criar o colaborador. Verifique se o CPF já existe.");
                }
            }
            ViewBag.Coordenadores = new SelectList(GetCoordenadores(), "CPF", "Nome", colaborador.CoordenadorCPF);
            return View(colaborador);
        }

        // GET: Colaboradores/Edit/5
        [Authorize(Roles = "Admin")]
        public IActionResult Edit(string id)
        {
            if (id == null) return NotFound();
            Colaborador colaborador = FindColaboradorById(SanitizeCpf(id));
            if (colaborador == null) return NotFound();
            ViewBag.Coordenadores = new SelectList(GetCoordenadores(), "CPF", "Nome", colaborador.CoordenadorCPF);
            return View(colaborador);
        }

        // POST: Colaboradores/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string id, Colaborador colaborador)
        {
            var sanitizedId = SanitizeCpf(id);
            colaborador.CPF = SanitizeCpf(colaborador.CPF);
            colaborador.CoordenadorCPF = SanitizeCpf(colaborador.CoordenadorCPF);

            if (sanitizedId != colaborador.CPF) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var oldColaborador = FindColaboradorById(sanitizedId);

                    using (var connection = _databaseService.CreateConnection())
                    {
                        connection.Open();
                        string sql = @"UPDATE Colaboradores SET 
                                       Nome = @Nome, Email = @Email, SenhaEmail = @SenhaEmail, Teams = @Teams, SenhaTeams = @SenhaTeams, 
                                       EDespacho = @EDespacho, SenhaEDespacho = @SenhaEDespacho, Genius = @Genius, SenhaGenius = @SenhaGenius, 
                                       Ibrooker = @Ibrooker, SenhaIbrooker = @SenhaIbrooker, Adicional = @Adicional, SenhaAdicional = @SenhaAdicional, 
                                       Filial = @Filial, Setor = @Setor, Smartphone = @Smartphone, TelefoneFixo = @TelefoneFixo, Ramal = @Ramal, Alarme = @Alarme, Videoporteiro = @Videoporteiro,
                                       Obs = @Obs, DataAlteracao = @DataAlteracao, CoordenadorCPF = @CoordenadorCPF
                                       WHERE CPF = @CPF";
                        using (var cmd = connection.CreateCommand())
                        {
                            cmd.CommandText = sql;
                            AddColaboradorParameters(cmd, colaborador);
                            var pDate = cmd.CreateParameter(); pDate.ParameterName = "@DataAlteracao"; pDate.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); cmd.Parameters.Add(pDate);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    await _persistentLogService.LogChangeAsync("Colaborador", "Update", User.Identity.Name, oldColaborador, colaborador);

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao editar colaborador.");
                    ModelState.AddModelError(string.Empty, "Ocorreu um erro ao editar o colaborador.");
                }
            }
            ViewBag.Coordenadores = new SelectList(GetCoordenadores(), "CPF", "Nome", colaborador.CoordenadorCPF);
            return View(colaborador);
        }

        // GET: Colaboradores/Delete/5
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(string id)
        {
            if (id == null) return NotFound();
            Colaborador colaborador = FindColaboradorById(SanitizeCpf(id));
            if (colaborador == null) return NotFound();
            return View(colaborador);
        }

        // POST: Colaboradores/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var sanitizedId = SanitizeCpf(id);
            try
            {
                var colaborador = FindColaboradorById(sanitizedId);
                if (colaborador != null)
                {
                    using (var connection = _databaseService.CreateConnection())
                    {
                        connection.Open();
                        string sql = "DELETE FROM Colaboradores WHERE CPF = @CPF";
                        using (var cmd = connection.CreateCommand())
                        {
                            cmd.CommandText = sql;
                            var p1 = cmd.CreateParameter(); p1.ParameterName = "@CPF"; p1.Value = sanitizedId; cmd.Parameters.Add(p1);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    await _persistentLogService.LogChangeAsync("Colaborador", "Delete", User.Identity.Name, colaborador, null);
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao excluir colaborador.");
                Colaborador colaborador = FindColaboradorById(sanitizedId);
                ViewBag.ErrorMessage = "Erro ao excluir. Verifique se o colaborador está associado a computadores, monitores ou periféricos.";
                return View(colaborador);
            }
        }

        private Colaborador FindColaboradorById(string id, IDbConnection connection = null, IDbTransaction transaction = null)
        {
            Colaborador colaborador = null;
            bool ownConnection = false;
            if (connection == null)
            {
                connection = _databaseService.CreateConnection();
                ownConnection = true;
            }

            try
            {
                if (ownConnection)
                {
                    connection.Open();
                }

                string sql = "SELECT * FROM Colaboradores WHERE CPF = @CPF";
                using (var cmd = connection.CreateCommand())
                {
                    cmd.Transaction = transaction;
                    cmd.CommandText = sql;
                    var p1 = cmd.CreateParameter(); p1.ParameterName = "@CPF"; p1.Value = id; cmd.Parameters.Add(p1);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            colaborador = new Colaborador
                            {
                                CPF = reader["CPF"].ToString(),
                                Nome = reader["Nome"].ToString(),
                                Email = reader["Email"].ToString(),
                                SenhaEmail = reader["SenhaEmail"].ToString(),
                                Teams = reader["Teams"].ToString(),
                                SenhaTeams = reader["SenhaTeams"].ToString(),
                                EDespacho = reader["EDespacho"].ToString(),
                                SenhaEDespacho = reader["SenhaEDespacho"].ToString(),
                                Genius = reader["Genius"].ToString(),
                                SenhaGenius = reader["SenhaGenius"].ToString(),
                                Ibrooker = reader["Ibrooker"].ToString(),
                                SenhaIbrooker = reader["SenhaIbrooker"].ToString(),
                                Adicional = reader["Adicional"].ToString(),
                                SenhaAdicional = reader["SenhaAdicional"].ToString(),
                                Filial = reader["Filial"].ToString(),
                                Setor = reader["Setor"].ToString(),
                                Smartphone = reader["Smartphone"].ToString(),
                                TelefoneFixo = reader["TelefoneFixo"].ToString(),
                                Ramal = reader["Ramal"].ToString(),
                                Alarme = reader["Alarme"].ToString(),
                                Videoporteiro = reader["Videoporteiro"].ToString(),
                                Obs = reader["Obs"].ToString(),
                                DataInclusao = reader["DataInclusao"] != DBNull.Value ? Convert.ToDateTime(reader["DataInclusao"]) : (DateTime?)null,
                                DataAlteracao = reader["DataAlteracao"] != DBNull.Value ? Convert.ToDateTime(reader["DataAlteracao"]) : (DateTime?)null,
                                CoordenadorCPF = reader["CoordenadorCPF"] != DBNull.Value ? reader["CoordenadorCPF"].ToString() : null
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao encontrar colaborador por ID.");
                if (transaction != null) throw;
            }
            finally
            {
                if (ownConnection)
                {
                    connection.Close();
                }
            }
            return colaborador;
        }

        private List<Colaborador> GetCoordenadores()
        {
            var coordenadores = new List<Colaborador>();
            try
            {
                using (var connection = _databaseService.CreateConnection())
                {
                    connection.Open();
                    string sql = "SELECT c.CPF, c.Nome FROM Colaboradores c INNER JOIN Usuarios u ON c.CPF = u.ColaboradorCPF WHERE u.Role = 'Coordenador' OR u.IsCoordinator = 1 ORDER BY c.Nome";
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = sql;
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                coordenadores.Add(new Colaborador
                                {
                                    CPF = reader["CPF"].ToString(),
                                    Nome = reader["Nome"].ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter a lista de coordenadores.");
            }
            return coordenadores;
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

            var colaboradores = new List<Colaborador>();
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
                            var colaborador = new Colaborador
                            {
                                CPF = SanitizeCpf(worksheet.Cells[row, 1].Value?.ToString().Trim()),
                                Nome = worksheet.Cells[row, 2].Value?.ToString().Trim(),
                                Email = worksheet.Cells[row, 3].Value?.ToString().Trim(),
                                SenhaEmail = worksheet.Cells[row, 4].Value?.ToString().Trim(),
                                Teams = worksheet.Cells[row, 5].Value?.ToString().Trim(),
                                SenhaTeams = worksheet.Cells[row, 6].Value?.ToString().Trim(),
                                EDespacho = worksheet.Cells[row, 7].Value?.ToString().Trim(),
                                SenhaEDespacho = worksheet.Cells[row, 8].Value?.ToString().Trim(),
                                Genius = worksheet.Cells[row, 9].Value?.ToString().Trim(),
                                SenhaGenius = worksheet.Cells[row, 10].Value?.ToString().Trim(),
                                Ibrooker = worksheet.Cells[row, 11].Value?.ToString().Trim(),
                                SenhaIbrooker = worksheet.Cells[row, 12].Value?.ToString().Trim(),
                                Adicional = worksheet.Cells[row, 13].Value?.ToString().Trim(),
                                SenhaAdicional = worksheet.Cells[row, 14].Value?.ToString().Trim(),
                                Filial = worksheet.Cells[row, 15].Value?.ToString().Trim(),
                                Setor = worksheet.Cells[row, 16].Value?.ToString().Trim(),
                                Smartphone = worksheet.Cells[row, 17].Value?.ToString().Trim(),
                                TelefoneFixo = worksheet.Cells[row, 18].Value?.ToString().Trim(),
                                Ramal = worksheet.Cells[row, 19].Value?.ToString().Trim(),
                                Alarme = worksheet.Cells[row, 20].Value?.ToString().Trim(),
                                Videoporteiro = worksheet.Cells[row, 21].Value?.ToString().Trim(),
                                Obs = worksheet.Cells[row, 22].Value?.ToString().Trim(),
                                CoordenadorCPF = SanitizeCpf(worksheet.Cells[row, 23].Value?.ToString().Trim())
                            };

                            if (!string.IsNullOrWhiteSpace(colaborador.CPF) && !string.IsNullOrWhiteSpace(colaborador.Nome))
                            {
                                colaboradores.Add(colaborador);
                            }
                        }
                    }
                }

                int adicionados = 0;
                int atualizados = 0;

                using (var connection = _databaseService.CreateConnection())
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            foreach (var colaborador in colaboradores)
                            {
                                var existente = FindColaboradorById(colaborador.CPF, connection, transaction);

                                if (existente != null)
                                {
                                    // Consider adding logging for bulk updates here if needed, but it might be too verbose.
                                    string updateSql = @"UPDATE Colaboradores SET 
                                                       Nome = @Nome, Email = @Email, SenhaEmail = @SenhaEmail, Teams = @Teams, SenhaTeams = @SenhaTeams, 
                                                       EDespacho = @EDespacho, SenhaEDespacho = @SenhaEDespacho, Genius = @Genius, SenhaGenius = @SenhaGenius, 
                                                       Ibrooker = @Ibrooker, SenhaIbrooker = @SenhaIbrooker, Adicional = @Adicional, SenhaAdicional = @SenhaAdicional, 
                                                       Filial = @Filial, Setor = @Setor, Smartphone = @Smartphone, TelefoneFixo = @TelefoneFixo, Ramal = @Ramal, Alarme = @Alarme, Videoporteiro = @Videoporteiro,
                                                       Obs = @Obs, DataAlteracao = @DataAlteracao, CoordenadorCPF = @CoordenadorCPF
                                                       WHERE CPF = @CPF";
                                    using (var cmd = connection.CreateCommand())
                                    {
                                        cmd.Transaction = transaction;
                                        cmd.CommandText = updateSql;
                                        AddColaboradorParameters(cmd, colaborador);
                                        var pDate = cmd.CreateParameter(); pDate.ParameterName = "@DataAlteracao"; pDate.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); cmd.Parameters.Add(pDate);
                                        cmd.ExecuteNonQuery();
                                    }
                                    atualizados++;
                                }
                                else
                                {
                                    string insertSql = @"INSERT INTO Colaboradores (CPF, Nome, Email, SenhaEmail, Teams, SenhaTeams, EDespacho, SenhaEDespacho, Genius, SenhaGenius, Ibrooker, SenhaIbrooker, Adicional, SenhaAdicional, Filial, Setor, Smartphone, TelefoneFixo, Ramal, Alarme, Videoporteiro, Obs, DataInclusao, CoordenadorCPF) 
                                                       VALUES (@CPF, @Nome, @Email, @SenhaEmail, @Teams, @SenhaTeams, @EDespacho, @SenhaEDespacho, @Genius, @SenhaGenius, @Ibrooker, @SenhaIbrooker, @Adicional, @SenhaAdicional, @Filial, @Setor, @Smartphone, @TelefoneFixo, @Ramal, @Alarme, @Videoporteiro, @Obs, @DataInclusao, @CoordenadorCPF)";
                                    using (var cmd = connection.CreateCommand())
                                    {
                                        cmd.Transaction = transaction;
                                        cmd.CommandText = insertSql;
                                        AddColaboradorParameters(cmd, colaborador);
                                        var pDate = cmd.CreateParameter(); pDate.ParameterName = "@DataInclusao"; pDate.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); cmd.Parameters.Add(pDate);
                                        cmd.ExecuteNonQuery();
                                    }
                                    adicionados++;
                                }
                            }
                            transaction.Commit();
                            TempData["SuccessMessage"] = $"{adicionados} colaboradores adicionados e {atualizados} atualizados com sucesso.";
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

        private List<string> GetDistinctColaboradorValues(IDbConnection connection, string columnName)
        {
            var values = new List<string>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = $"SELECT DISTINCT {columnName} FROM Colaboradores WHERE {columnName} IS NOT NULL ORDER BY {columnName}";
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

        private void AddColaboradorParameters(IDbCommand cmd, Colaborador colaborador)
        {
            var p1 = cmd.CreateParameter(); p1.ParameterName = "@CPF"; p1.Value = colaborador.CPF; cmd.Parameters.Add(p1);
            var p2 = cmd.CreateParameter(); p2.ParameterName = "@Nome"; p2.Value = colaborador.Nome; cmd.Parameters.Add(p2);
            var p3 = cmd.CreateParameter(); p3.ParameterName = "@Email"; p3.Value = (object)colaborador.Email ?? DBNull.Value; cmd.Parameters.Add(p3);
            var p4 = cmd.CreateParameter(); p4.ParameterName = "@SenhaEmail"; p4.Value = (object)colaborador.SenhaEmail ?? DBNull.Value; cmd.Parameters.Add(p4);
            var p5 = cmd.CreateParameter(); p5.ParameterName = "@Teams"; p5.Value = (object)colaborador.Teams ?? DBNull.Value; cmd.Parameters.Add(p5);
            var p6 = cmd.CreateParameter(); p6.ParameterName = "@SenhaTeams"; p6.Value = (object)colaborador.SenhaTeams ?? DBNull.Value; cmd.Parameters.Add(p6);
            var p7 = cmd.CreateParameter(); p7.ParameterName = "@EDespacho"; p7.Value = (object)colaborador.EDespacho ?? DBNull.Value; cmd.Parameters.Add(p7);
            var p8 = cmd.CreateParameter(); p8.ParameterName = "@SenhaEDespacho"; p8.Value = (object)colaborador.SenhaEDespacho ?? DBNull.Value; cmd.Parameters.Add(p8);
            var p9 = cmd.CreateParameter(); p9.ParameterName = "@Genius"; p9.Value = (object)colaborador.Genius ?? DBNull.Value; cmd.Parameters.Add(p9);
            var p10 = cmd.CreateParameter(); p10.ParameterName = "@SenhaGenius"; p10.Value = (object)colaborador.SenhaGenius ?? DBNull.Value; cmd.Parameters.Add(p10);
            var p11 = cmd.CreateParameter(); p11.ParameterName = "@Ibrooker"; p11.Value = (object)colaborador.Ibrooker ?? DBNull.Value; cmd.Parameters.Add(p11);
            var p12 = cmd.CreateParameter(); p12.ParameterName = "@SenhaIbrooker"; p12.Value = (object)colaborador.SenhaIbrooker ?? DBNull.Value; cmd.Parameters.Add(p12);
            var p13 = cmd.CreateParameter(); p13.ParameterName = "@Adicional"; p13.Value = (object)colaborador.Adicional ?? DBNull.Value; cmd.Parameters.Add(p13);
            var p14 = cmd.CreateParameter(); p14.ParameterName = "@SenhaAdicional"; p14.Value = (object)colaborador.SenhaAdicional ?? DBNull.Value; cmd.Parameters.Add(p14);
            var p15 = cmd.CreateParameter(); p15.ParameterName = "@Filial"; p15.Value = (object)colaborador.Filial ?? DBNull.Value; cmd.Parameters.Add(p15);
            var p16 = cmd.CreateParameter(); p16.ParameterName = "@Setor"; p16.Value = (object)colaborador.Setor ?? DBNull.Value; cmd.Parameters.Add(p16);
            var p17 = cmd.CreateParameter(); p17.ParameterName = "@Smartphone"; p17.Value = (object)colaborador.Smartphone ?? DBNull.Value; cmd.Parameters.Add(p17);
            var p18 = cmd.CreateParameter(); p18.ParameterName = "@TelefoneFixo"; p18.Value = (object)colaborador.TelefoneFixo ?? DBNull.Value; cmd.Parameters.Add(p18);
            var p19 = cmd.CreateParameter(); p19.ParameterName = "@Ramal"; p19.Value = (object)colaborador.Ramal ?? DBNull.Value; cmd.Parameters.Add(p19);
            var p20 = cmd.CreateParameter(); p20.ParameterName = "@Alarme"; p20.Value = (object)colaborador.Alarme ?? DBNull.Value; cmd.Parameters.Add(p20);
            var p21 = cmd.CreateParameter(); p21.ParameterName = "@Videoporteiro"; p21.Value = (object)colaborador.Videoporteiro ?? DBNull.Value; cmd.Parameters.Add(p21);
            var p22 = cmd.CreateParameter(); p22.ParameterName = "@Obs"; p22.Value = (object)colaborador.Obs ?? DBNull.Value; cmd.Parameters.Add(p22);
            var p23 = cmd.CreateParameter(); p23.ParameterName = "@CoordenadorCPF"; p23.Value = (object)colaborador.CoordenadorCPF ?? DBNull.Value; cmd.Parameters.Add(p23);
        }
    }
}
