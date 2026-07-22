using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Web.Models;
using Web.Services;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin,Diretoria/RH")]
    public class ProgramasController : Controller
    {
        private readonly IDatabaseService _databaseService;
        private readonly ComandoService _comandoService;
        private readonly ILogger<ProgramasController> _logger;
        private readonly LogService _logService;

        public ProgramasController(IDatabaseService databaseService, ComandoService comandoService, ILogger<ProgramasController> logger, LogService logService)
        {
            _databaseService = databaseService;
            _comandoService = comandoService;
            _logger = logger;
            _logService = logService;
        }

        public IActionResult Index(string filter = null)
        {
            var programas = new List<ProgramaInstalado>();
            using (var connection = _databaseService.CreateConnection())
            {
                connection.Open();
                var sql = @"SELECT p.*, c.Hostname
                            FROM ProgramasInstalados p
                            JOIN Computadores c ON p.ComputadorMAC = c.MAC";

                if (!string.IsNullOrEmpty(filter))
                {
                    sql += " WHERE p.Nome LIKE @Filter OR c.Hostname LIKE @Filter";
                }

                sql += " ORDER BY p.Nome ASC";

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = sql;
                    if (!string.IsNullOrEmpty(filter))
                    {
                        var p = cmd.CreateParameter();
                        p.ParameterName = "@Filter";
                        p.Value = "%" + filter + "%";
                        cmd.Parameters.Add(p);
                    }

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            programas.Add(new ProgramaInstalado
                            {
                                ID = Convert.ToInt32(reader["ID"]),
                                ComputadorMAC = reader["ComputadorMAC"].ToString(),
                                Nome = reader["Nome"].ToString(),
                                Versao = reader["Versao"] != DBNull.Value ? reader["Versao"].ToString() : "",
                                Desenvolvedor = reader["Desenvolvedor"] != DBNull.Value ? reader["Desenvolvedor"].ToString() : "",
                                DataColeta = Convert.ToDateTime(reader["DataColeta"]),
                                Hostname = reader["Hostname"].ToString()
                            });
                        }
                    }
                }
            }

            ViewBag.Filter = filter;
            return View(programas);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ColetarProgramas(string mac, string ip)
        {
            if (string.IsNullOrEmpty(mac) || string.IsNullOrEmpty(ip))
            {
                TempData["ErrorMessage"] = "MAC e IP são obrigatórios para a coleta remota.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                _logService.AddLog("Info", $"Iniciando coleta de programas para {ip} ({mac})", "Programas");
                // The actual processing should happen in background or via SignalR, here we trigger and wait
                // Due to timeout limits in synchronous web requests, we will just send the command.
                // The agent needs to return a large JSON array which we should parse.

                // Fire and forget or handle properly?
                // Let's create a background task or use a dedicated method
                Task.Run(async () =>
                {
                    try {
                        string resultado = await _comandoService.EnviarComandoAsync(ip, "get_installed_programs");
                        _logger.LogInformation("Recebeu resposta do comando get_installed_programs");

                        // Limpa a string para pegar apenas o JSON (ignora mensagens de erro/aviso que podem vir do PowerShell)
                        string jsonToParse = resultado;
                        int firstBrace = resultado.IndexOf('{');
                        int firstBracket = resultado.IndexOf('[');

                        if (firstBrace != -1 || firstBracket != -1)
                        {
                            int startIdx = -1;
                            if (firstBrace != -1 && firstBracket != -1) startIdx = Math.Min(firstBrace, firstBracket);
                            else if (firstBrace != -1) startIdx = firstBrace;
                            else startIdx = firstBracket;

                            jsonToParse = resultado.Substring(startIdx);
                        }

                        if (string.IsNullOrWhiteSpace(jsonToParse) || (!jsonToParse.TrimStart().StartsWith("{") && !jsonToParse.TrimStart().StartsWith("[")))
                        {
                            _logger.LogWarning($"Resultado vazio ou inválido recebido de {ip}. Resposta original: {resultado}");
                            _logService.AddLog("Warning", $"Resultado inválido recebido ao tentar coletar programas de {ip}. Não foi possível extrair um formato JSON válido.", "Programas");
                            return; // Encerra task silenciosamente ou pode logar no db
                        }

                        // Parse JSON result and save to DB
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var jsonDoc = JsonDocument.Parse(jsonToParse);

                        var programas = new List<ProgramaInfo>();
                        if (jsonDoc.RootElement.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var element in jsonDoc.RootElement.EnumerateArray())
                            {
                                programas.Add(new ProgramaInfo
                                {
                                    DisplayName = element.GetProperty("DisplayName").GetString(),
                                    DisplayVersion = element.TryGetProperty("DisplayVersion", out var v) ? v.GetString() : "",
                                    Publisher = element.TryGetProperty("Publisher", out var p) ? p.GetString() : ""
                                });
                            }
                        }
                        else if (jsonDoc.RootElement.ValueKind == JsonValueKind.Object)
                        {
                            programas.Add(new ProgramaInfo
                            {
                                DisplayName = jsonDoc.RootElement.GetProperty("DisplayName").GetString(),
                                DisplayVersion = jsonDoc.RootElement.TryGetProperty("DisplayVersion", out var v) ? v.GetString() : "",
                                Publisher = jsonDoc.RootElement.TryGetProperty("Publisher", out var p) ? p.GetString() : ""
                            });
                        }

                        if (programas.Count > 0)
                        {
                            using (var connection = _databaseService.CreateConnection())
                            {
                                connection.Open();
                                // First clear old ones
                                using (var cmdDel = connection.CreateCommand())
                                {
                                    cmdDel.CommandText = "DELETE FROM ProgramasInstalados WHERE ComputadorMAC = @MAC";
                                    var pDel = cmdDel.CreateParameter(); pDel.ParameterName = "@MAC"; pDel.Value = mac; cmdDel.Parameters.Add(pDel);
                                    cmdDel.ExecuteNonQuery();
                                }

                                // Insert new
                                var dataColeta = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                                foreach (var p in programas)
                                {
                                    using (var cmd = connection.CreateCommand())
                                    {
                                        cmd.CommandText = "INSERT INTO ProgramasInstalados (ComputadorMAC, Nome, Versao, Desenvolvedor, DataColeta) VALUES (@MAC, @Nome, @Versao, @Desenvolvedor, @DataColeta)";
                                        var p1 = cmd.CreateParameter(); p1.ParameterName = "@MAC"; p1.Value = mac; cmd.Parameters.Add(p1);
                                        var p2 = cmd.CreateParameter(); p2.ParameterName = "@Nome"; p2.Value = p.DisplayName; cmd.Parameters.Add(p2);
                                        var p3 = cmd.CreateParameter(); p3.ParameterName = "@Versao"; p3.Value = p.DisplayVersion ?? (object)DBNull.Value; cmd.Parameters.Add(p3);
                                        var p4 = cmd.CreateParameter(); p4.ParameterName = "@Desenvolvedor"; p4.Value = p.Publisher ?? (object)DBNull.Value; cmd.Parameters.Add(p4);
                                        var p5 = cmd.CreateParameter(); p5.ParameterName = "@DataColeta"; p5.Value = dataColeta; cmd.Parameters.Add(p5);
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                            }
                            _logService.AddLog("Success", $"Coleta de programas para {ip} concluída com sucesso ({programas.Count} programas).", "Programas");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro na coleta de programas em background");
                        _logService.AddLog("Error", $"Erro na coleta de programas para {ip}: {ex.Message}. Verifique os logs do servidor para mais detalhes.", "Programas");
                    }
                });

                TempData["SuccessMessage"] = $"Comando enviado para {ip}. Os dados serão processados em background.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao iniciar coleta de programas.");
                TempData["ErrorMessage"] = $"Erro ao enviar comando: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        private class ProgramaInfo
        {
            public string DisplayName { get; set; }
            public string DisplayVersion { get; set; }
            public string Publisher { get; set; }
        }
    }
}
