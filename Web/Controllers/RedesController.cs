using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Web.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Web.Services;
using System.Data;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using System.IO;
using System.Threading.Tasks;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class RedesController : Controller
    {
        private readonly IDatabaseService _databaseService;
        private readonly ILogger<RedesController> _logger;
        private readonly PersistentLogService _persistentLogService;

        public RedesController(IDatabaseService databaseService, ILogger<RedesController> logger, PersistentLogService persistentLogService)
        {
            _databaseService = databaseService;
            _logger = logger;
            _persistentLogService = persistentLogService;
        }

        public IActionResult Index()
        {
            var redes = new List<Rede>();
            try
            {
                using (var connection = _databaseService.CreateConnection())
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT * FROM Rede";
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                redes.Add(new Rede
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    Tipo = reader["Tipo"].ToString(),
                                    IP = reader["IP"].ToString(),
                                    MAC = reader["MAC"].ToString(),
                                    Nome = reader["Nome"].ToString(),
                                    DataInclusao = Convert.ToDateTime(reader["DataInclusao"]),
                                    DataAlteracao = reader["DataAlteracao"] != DBNull.Value ? Convert.ToDateTime(reader["DataAlteracao"]) : (DateTime?)null,
                                    Observacao = reader["Observacao"].ToString()
                                });
                            }
                        }
                    }
                }

                // Sort the list in-memory using System.Version for correct IP sorting
                // As before, this is risky if IPs are malformed
                var redesOrdenadas = redes.OrderBy<Rede, long>(r =>
                {
                     if (System.Net.IPAddress.TryParse(r.IP, out var ip))
                     {
                         var bytes = ip.GetAddressBytes();
                         if (bytes.Length == 4)
                         {
                             return (long)BitConverter.ToUInt32(bytes.Reverse().ToArray(), 0);
                         }
                     }
                     return 0L;
                }).ToList();
                return View(redesOrdenadas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting network assets list.");
                // Handle error appropriately
                return View(redes); // Return unsorted list in case of an error
            }
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Rede rede)
        {
            _logger.LogInformation("Create POST action called for network asset.");
            if (ModelState.IsValid)
            {
                _logger.LogInformation("ModelState is valid. Attempting to save to the database.");
                try
                {
                    using (var connection = _databaseService.CreateConnection())
                    {
                        connection.Open();
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = "INSERT INTO Rede (Tipo, IP, MAC, Nome, DataInclusao, Observacao) VALUES (@Tipo, @IP, @MAC, @Nome, @DataInclusao, @Observacao)";

                            var p1 = command.CreateParameter(); p1.ParameterName = "@Tipo"; p1.Value = rede.Tipo; command.Parameters.Add(p1);
                            var p2 = command.CreateParameter(); p2.ParameterName = "@IP"; p2.Value = rede.IP; command.Parameters.Add(p2);
                            var p3 = command.CreateParameter(); p3.ParameterName = "@MAC"; p3.Value = (object)rede.MAC ?? DBNull.Value; command.Parameters.Add(p3);
                            var p4 = command.CreateParameter(); p4.ParameterName = "@Nome"; p4.Value = rede.Nome; command.Parameters.Add(p4);
                            var p5 = command.CreateParameter(); p5.ParameterName = "@DataInclusao"; p5.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); command.Parameters.Add(p5);
                            var p6 = command.CreateParameter(); p6.ParameterName = "@Observacao"; p6.Value = (object)rede.Observacao ?? DBNull.Value; command.Parameters.Add(p6);

                            _logger.LogInformation("Executing INSERT command for network asset '{Nome}'.", rede.Nome);
                            command.ExecuteNonQuery();
                            _logger.LogInformation("INSERT command executed successfully.");

                            await _persistentLogService.LogChangeAsync(
                                User.Identity.Name,
                                "CREATE",
                                "Rede",
                                $"Created network asset: {rede.Nome}, IP: {rede.IP}",
                                $"Tipo: {rede.Tipo}, IP: {rede.IP}, MAC: {rede.MAC}"
                            );
                        }
                    }
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating network asset in the database.");
                    ModelState.AddModelError(string.Empty, "Ocorreu um erro ao salvar os dados. Por favor, tente novamente.");
                }
            }
            else
            {
                _logger.LogWarning("ModelState is invalid. Validation errors:");
                foreach (var state in ModelState)
                {
                    if (state.Value.Errors.Any())
                    {
                        var errors = string.Join(", ", state.Value.Errors.Select(e => e.ErrorMessage));
                        _logger.LogWarning($"- {state.Key}: {errors}");
                    }
                }
            }
            _logger.LogInformation("Returning view with model due to validation errors or exception.");
            return View(rede);
        }

        public IActionResult Edit(int id)
        {
            var rede = FindRedeById(id);
            if (rede == null)
            {
                return NotFound();
            }
            return View(rede);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Rede rede)
        {
            if (id != rede.Id)
            {
                return NotFound();
            }

            _logger.LogInformation("Edit POST action called for network asset ID {Id}.", id);
            if (ModelState.IsValid)
            {
                _logger.LogInformation("ModelState is valid. Attempting to update the database.");
                try
                {
                    using (var connection = _databaseService.CreateConnection())
                    {
                        connection.Open();
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = "UPDATE Rede SET Tipo = @Tipo, IP = @IP, MAC = @MAC, Nome = @Nome, DataAlteracao = @DataAlteracao, Observacao = @Observacao WHERE Id = @Id";

                            var p1 = command.CreateParameter(); p1.ParameterName = "@Id"; p1.Value = rede.Id; command.Parameters.Add(p1);
                            var p2 = command.CreateParameter(); p2.ParameterName = "@Tipo"; p2.Value = rede.Tipo; command.Parameters.Add(p2);
                            var p3 = command.CreateParameter(); p3.ParameterName = "@IP"; p3.Value = rede.IP; command.Parameters.Add(p3);
                            var p4 = command.CreateParameter(); p4.ParameterName = "@MAC"; p4.Value = (object)rede.MAC ?? DBNull.Value; command.Parameters.Add(p4);
                            var p5 = command.CreateParameter(); p5.ParameterName = "@Nome"; p5.Value = rede.Nome; command.Parameters.Add(p5);
                            var p6 = command.CreateParameter(); p6.ParameterName = "@DataAlteracao"; p6.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); command.Parameters.Add(p6);
                            var p7 = command.CreateParameter(); p7.ParameterName = "@Observacao"; p7.Value = (object)rede.Observacao ?? DBNull.Value; command.Parameters.Add(p7);

                            _logger.LogInformation("Executing UPDATE command for network asset ID {Id}.", rede.Id);
                            command.ExecuteNonQuery();
                            _logger.LogInformation("UPDATE command executed successfully for ID {Id}.", rede.Id);

                            await _persistentLogService.LogChangeAsync(
                                User.Identity.Name,
                                "EDIT",
                                "Rede",
                                $"Updated network asset: {rede.Nome}, IP: {rede.IP}",
                                $"ID: {rede.Id}, Tipo: {rede.Tipo}, IP: {rede.IP}"
                            );
                        }
                    }
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating network asset in the database.");
                    ModelState.AddModelError(string.Empty, "Ocorreu um erro ao salvar os dados. Por favor, tente novamente.");
                }
            }
            else
            {
                _logger.LogWarning("ModelState is invalid. Validation errors:");
                foreach (var state in ModelState)
                {
                    if (state.Value.Errors.Any())
                    {
                        var errors = string.Join(", ", state.Value.Errors.Select(e => e.ErrorMessage));
                        _logger.LogWarning($"- {state.Key}: {errors}");
                    }
                }
            }
            _logger.LogInformation("Returning view with model due to validation errors or exception.");
            return View(rede);
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

            var redes = new List<Rede>();
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
                            var rede = new Rede
                            {
                                Tipo = worksheet.Cells[row, 1].Value?.ToString().Trim(),
                                IP = worksheet.Cells[row, 2].Value?.ToString().Trim(),
                                MAC = worksheet.Cells[row, 3].Value?.ToString().Trim(),
                                Nome = worksheet.Cells[row, 4].Value?.ToString().Trim(),
                                Observacao = worksheet.Cells[row, 5].Value?.ToString().Trim()
                            };

                            if (!string.IsNullOrWhiteSpace(rede.Tipo) && !string.IsNullOrWhiteSpace(rede.IP) && !string.IsNullOrWhiteSpace(rede.Nome))
                            {
                                redes.Add(rede);
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
                            foreach (var rede in redes)
                            {
                                var existente = FindRedeByMacOrIp(rede.MAC, rede.IP, connection, transaction);

                                if (existente != null)
                                {
                                    string updateSql = @"UPDATE Rede SET
                                                       Tipo = @Tipo, IP = @IP, MAC = @MAC, Nome = @Nome, Observacao = @Observacao, DataAlteracao = @DataAlteracao
                                                       WHERE Id = @Id";
                                    using (var cmd = connection.CreateCommand())
                                    {
                                        cmd.Transaction = transaction;
                                        cmd.CommandText = updateSql;
                                        var p1 = cmd.CreateParameter(); p1.ParameterName = "@Id"; p1.Value = existente.Id; cmd.Parameters.Add(p1);
                                        var p2 = cmd.CreateParameter(); p2.ParameterName = "@Tipo"; p2.Value = rede.Tipo; cmd.Parameters.Add(p2);
                                        var p3 = cmd.CreateParameter(); p3.ParameterName = "@IP"; p3.Value = rede.IP; cmd.Parameters.Add(p3);
                                        var p4 = cmd.CreateParameter(); p4.ParameterName = "@MAC"; p4.Value = (object)rede.MAC ?? DBNull.Value; cmd.Parameters.Add(p4);
                                        var p5 = cmd.CreateParameter(); p5.ParameterName = "@Nome"; p5.Value = rede.Nome; cmd.Parameters.Add(p5);
                                        var p6 = cmd.CreateParameter(); p6.ParameterName = "@Observacao"; p6.Value = (object)rede.Observacao ?? DBNull.Value; cmd.Parameters.Add(p6);
                                        var p7 = cmd.CreateParameter(); p7.ParameterName = "@DataAlteracao"; p7.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); cmd.Parameters.Add(p7);
                                        cmd.ExecuteNonQuery();
                                    }
                                    atualizados++;
                                }
                                else
                                {
                                    string insertSql = @"INSERT INTO Rede (Tipo, IP, MAC, Nome, Observacao, DataInclusao)
                                                       VALUES (@Tipo, @IP, @MAC, @Nome, @Observacao, @DataInclusao)";
                                    using (var cmd = connection.CreateCommand())
                                    {
                                        cmd.Transaction = transaction;
                                        cmd.CommandText = insertSql;
                                        var p1 = cmd.CreateParameter(); p1.ParameterName = "@Tipo"; p1.Value = rede.Tipo; cmd.Parameters.Add(p1);
                                        var p2 = cmd.CreateParameter(); p2.ParameterName = "@IP"; p2.Value = rede.IP; cmd.Parameters.Add(p2);
                                        var p3 = cmd.CreateParameter(); p3.ParameterName = "@MAC"; p3.Value = (object)rede.MAC ?? DBNull.Value; cmd.Parameters.Add(p3);
                                        var p4 = cmd.CreateParameter(); p4.ParameterName = "@Nome"; p4.Value = rede.Nome; cmd.Parameters.Add(p4);
                                        var p5 = cmd.CreateParameter(); p5.ParameterName = "@Observacao"; p5.Value = (object)rede.Observacao ?? DBNull.Value; cmd.Parameters.Add(p5);
                                        var p6 = cmd.CreateParameter(); p6.ParameterName = "@DataInclusao"; p6.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); cmd.Parameters.Add(p6);
                                        cmd.ExecuteNonQuery();
                                    }
                                    adicionados++;
                                }
                            }
                            transaction.Commit();
                            TempData["SuccessMessage"] = $"{adicionados} ativos de rede adicionados e {atualizados} atualizados com sucesso.";
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

        private Rede FindRedeByMacOrIp(string mac, string ip, IDbConnection connection, IDbTransaction transaction)
        {
            Rede rede = null;
            try
            {
                string sql = "SELECT * FROM Rede WHERE MAC = @MAC OR IP = @IP LIMIT 1";
                if (string.IsNullOrWhiteSpace(mac)) {
                    sql = "SELECT * FROM Rede WHERE IP = @IP LIMIT 1";
                }
                using (var cmd = connection.CreateCommand())
                {
                    cmd.Transaction = transaction;
                    cmd.CommandText = sql;
                    if (!string.IsNullOrWhiteSpace(mac)) {
                        var p1 = cmd.CreateParameter(); p1.ParameterName = "@MAC"; p1.Value = mac; cmd.Parameters.Add(p1);
                    }
                    var p2 = cmd.CreateParameter(); p2.ParameterName = "@IP"; p2.Value = ip; cmd.Parameters.Add(p2);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            rede = new Rede
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Tipo = reader["Tipo"].ToString(),
                                IP = reader["IP"].ToString(),
                                MAC = reader["MAC"].ToString(),
                                Nome = reader["Nome"].ToString(),
                                DataInclusao = Convert.ToDateTime(reader["DataInclusao"]),
                                DataAlteracao = reader["DataAlteracao"] != DBNull.Value ? Convert.ToDateTime(reader["DataAlteracao"]) : (DateTime?)null,
                                Observacao = reader["Observacao"].ToString()
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao encontrar rede por MAC ou IP.");
                if (transaction != null) throw;
            }
            return rede;
        }

        public IActionResult Delete(int id)
        {
            var rede = FindRedeById(id);
            if (rede == null)
            {
                return NotFound();
            }
            return View(rede);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var rede = FindRedeById(id);
                using (var connection = _databaseService.CreateConnection())
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "DELETE FROM Rede WHERE Id = @Id";
                        var p1 = command.CreateParameter(); p1.ParameterName = "@Id"; p1.Value = id; command.Parameters.Add(p1);
                        command.ExecuteNonQuery();

                        if (rede != null)
                        {
                            await _persistentLogService.LogChangeAsync(
                                User.Identity.Name,
                                "DELETE",
                                "Rede",
                                $"Deleted network asset: {rede.Nome}",
                                $"ID: {id}, Name: {rede.Nome}, IP: {rede.IP}"
                            );
                        }
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting network asset.");
                // Handle error
            }
            return RedirectToAction(nameof(Index));
        }

        private Rede FindRedeById(int id)
        {
            Rede rede = null;
            try
            {
                using (var connection = _databaseService.CreateConnection())
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT * FROM Rede WHERE Id = @Id";
                        var p1 = command.CreateParameter(); p1.ParameterName = "@Id"; p1.Value = id; command.Parameters.Add(p1);
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                rede = new Rede
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    Tipo = reader["Tipo"].ToString(),
                                    IP = reader["IP"].ToString(),
                                    MAC = reader["MAC"].ToString(),
                                    Nome = reader["Nome"].ToString(),
                                    DataInclusao = Convert.ToDateTime(reader["DataInclusao"]),
                                    DataAlteracao = reader["DataAlteracao"] != DBNull.Value ? Convert.ToDateTime(reader["DataAlteracao"]) : (DateTime?)null,
                                    Observacao = reader["Observacao"].ToString()
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding network asset by id.");
                // Handle error
            }
            return rede;
        }
    }
}
