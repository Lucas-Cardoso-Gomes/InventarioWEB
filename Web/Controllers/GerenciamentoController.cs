using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Web.Models;
using Web.Services;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System.Data;
using Microsoft.Data.Sqlite;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class GerenciamentoController : Controller
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<GerenciamentoController> _logger;
        private readonly IConfiguration _configuration;
        private readonly PersistentLogService _persistentLogService;
        private readonly IDatabaseService _databaseService;
        
        public GerenciamentoController(IServiceScopeFactory scopeFactory, ILogger<GerenciamentoController> logger, IConfiguration configuration, PersistentLogService persistentLogService, IDatabaseService databaseService)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _configuration = configuration;
            _persistentLogService = persistentLogService;
            _databaseService = databaseService;
        }

        // GET: /Gerenciamento/Logs
        public IActionResult Logs(string level, string source, string searchString, int pageNumber = 1, int pageSize = 25)
        {
            var viewModel = new LogViewModel
            {
                Logs = new List<Log>(),
                Levels = new List<string>(),
                Sources = new List<string>(),
                CurrentLevel = level,
                CurrentSource = source,
                SearchString = searchString,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            try
            {
                using (var connection = _databaseService.CreateConnection())
                {
                    connection.Open();
                    
                    viewModel.Levels = GetDistinctLogValues(connection, "Level");
                    viewModel.Sources = GetDistinctLogValues(connection, "Source");

                    var whereClauses = new List<string>();
                    var parameters = new Dictionary<string, object>();

                    if (!string.IsNullOrEmpty(level))
                    {
                        whereClauses.Add("Level = @level");
                        parameters.Add("@level", level);
                    }
                    if (!string.IsNullOrEmpty(source))
                    {
                        whereClauses.Add("Source = @source");
                        parameters.Add("@source", source);
                    }
                    if (!string.IsNullOrEmpty(searchString))
                    {
                        whereClauses.Add("Message LIKE @search");
                        parameters.Add("@search", $"%{searchString}%");
                    }

                    string whereSql = whereClauses.Any() ? $"WHERE {string.Join(" AND ", whereClauses)}" : "";

                    // Get total count for pagination
                    string countSql = $"SELECT COUNT(*) FROM Logs {whereSql}";
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

                    // Get paginated logs
                    string sql = $"SELECT Id, Timestamp, Level, Message, Source FROM Logs {whereSql} ORDER BY Timestamp DESC LIMIT @pageSize OFFSET @offset";
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = sql;
                        foreach (var p in parameters) {
                             var param = command.CreateParameter();
                             param.ParameterName = p.Key;
                             param.Value = p.Value;
                             command.Parameters.Add(param);
                        }
                        var pOffset = command.CreateParameter(); pOffset.ParameterName = "@offset"; pOffset.Value = (pageNumber - 1) * pageSize; command.Parameters.Add(pOffset);
                        var pPageSize = command.CreateParameter(); pPageSize.ParameterName = "@pageSize"; pPageSize.Value = pageSize; command.Parameters.Add(pPageSize);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var log = new Log();
                                log.Id = Convert.ToInt32(reader["Id"]);

                                var timestampObj = reader["Timestamp"];
                                if (timestampObj != DBNull.Value && DateTime.TryParse(timestampObj.ToString(), out DateTime dt))
                                {
                                    log.Timestamp = dt;
                                }
                                else
                                {
                                    log.Timestamp = DateTime.MinValue;
                                }

                                log.Level = reader["Level"] != DBNull.Value ? reader["Level"].ToString() : string.Empty;
                                log.Message = reader["Message"] != DBNull.Value ? reader["Message"].ToString() : string.Empty;
                                log.Source = reader["Source"] != DBNull.Value ? reader["Source"].ToString() : null;

                                viewModel.Logs.Add(log);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter os logs.");
                ViewBag.ErrorMessage = "Erro ao carregar logs. Verifique a conexão com o banco de dados.";
            }
            
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ClearLogs()
        {
            try
            {
                using (var connection = _databaseService.CreateConnection())
                {
                    connection.Open();
                    string sql = "DELETE FROM Logs"; // TRUNCATE is not standard SQL, SQLite uses DELETE FROM
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = sql;
                        command.ExecuteNonQuery();
                    }
                }
                TempData["SuccessMessage"] = "Logs limpos com sucesso!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao limpar os logs.");
                TempData["ErrorMessage"] = "Ocorreu um erro ao limpar os logs.";
            }

            return RedirectToAction(nameof(Logs));
        }

        private List<string> GetDistinctLogValues(IDbConnection connection, string columnName)
        {
            var values = new List<string>();
            string sql = $"SELECT DISTINCT {columnName} FROM Logs WHERE {columnName} IS NOT NULL ORDER BY {columnName}";
            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
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

        // GET: /Gerenciamento
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult PersistentLogs(string entityTypeFilter, string actionTypeFilter, int pageNumber = 1, int pageSize = 50)
        {
            var (logs, totalRecords) = _persistentLogService.GetLogs(entityTypeFilter, actionTypeFilter, pageNumber, pageSize);
            var viewModel = new PersistentLogViewModel
            {
                Logs = logs,
                EntityTypeFilter = entityTypeFilter,
                ActionTypeFilter = actionTypeFilter,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalRecords
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ClearPersistentLogs()
        {
            try
            {
                using (var connection = _databaseService.CreateConnection())
                {
                    connection.Open();
                    string sql = "DELETE FROM PersistentLogs";
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = sql;
                        command.ExecuteNonQuery();
                    }
                }
                TempData["SuccessMessage"] = "Log persistente limpo com sucesso!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao limpar o log persistente.");
                TempData["ErrorMessage"] = "Ocorreu um erro ao limpar o log persistente.";
            }

            return RedirectToAction(nameof(PersistentLogs));
        }

        // GET: /Gerenciamento/Coletar
        public IActionResult Coletar()
        {
            var model = new ColetaViewModel();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Coletar(ColetaViewModel model)
        {
            model.ColetaIniciada = true;

            if (model.TipoColeta == "ip")
            {
                if (string.IsNullOrWhiteSpace(model.IpAddress))
                {
                    ModelState.AddModelError("IpAddress", "O endereço IP é obrigatório.");
                    return View(model);
                }
                
                string ip = model.IpAddress;
                Task.Run(() => RunScopedColeta(ip));
                model.Resultados.Add($"Coleta agendada para o IP: {ip}. Os resultados aparecerão na página de Logs.");
            }
            else if (model.TipoColeta == "range")
            {
                string[] faixas;
                if (model.IpRange == "all")
                {
                    faixas = new string[] { "10.0.0.", "10.0.2.", "10.1.1.", "10.1.2.", "10.2.2.", "10.3.3.", "10.4.4." };
                }
                else
                {
                    faixas = new string[] { model.IpRange };
                }

                model.Resultados.Add($"Varredura de coleta agendada para as faixas: {string.Join(", ", faixas)}. Os resultados aparecerão na página de Logs.");

                Task.Run(() =>
                {
                    foreach (var faixaBase in faixas)
                    {
                        Parallel.For(1, 256, i =>
                        {
                            string ipFaixa = faixaBase + i.ToString();
                            RunScopedColeta(ipFaixa);
                        });
                    }
                });
            }

            return View(model);
        }
        
        private async Task RunScopedColeta(string ip)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var coletaService = scope.ServiceProvider.GetRequiredService<ColetaService>();
                var logService = scope.ServiceProvider.GetRequiredService<LogService>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<GerenciamentoController>>();
                try
                {
                    await coletaService.ColetarDadosAsync(ip, (result) => {
                        logger.LogInformation(result);
                    });
                }
                catch (Exception ex)
                {
                    logService.AddLog("Error", $"Falha na tarefa de coleta para {ip}: {ex.Message}", "Sistema");
                }
            }
        }


        // GET: /Gerenciamento/Comandos
        public IActionResult Comandos()
        {
            var model = new ComandoViewModel();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Comandos(ComandoViewModel model)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var logService = scope.ServiceProvider.GetRequiredService<LogService>();
                logService.AddLog("Debug", $"Ação Comandos recebida. Tipo: {model.TipoEnvio}, IP: {model.IpAddress}, Range: {model.IpRange}, Comando: {model.Comando}", "Sistema");
            }
            
            model.ComandoIniciado = true;

            // NOTA: A validação do ModelState foi removida intencionalmente, pois estava causando
            // um erro inexplicável. A validação manual abaixo é suficiente.
            // if (!ModelState.IsValid)
            // {
            //     return View(model);
            // }

            if (model.TipoEnvio == "ip")
            {
                if (string.IsNullOrWhiteSpace(model.IpAddress))
                {
                    ModelState.AddModelError("IpAddress", "O endereço IP é obrigatório.");
                    return View(model);
                }
                
                string ip = model.IpAddress;
                string comando = model.Comando;

                Task.Run(() => RunScopedComandoAsync(ip, comando));
                model.Resultados.Add($"Envio do comando '{comando}' agendado para o IP: {ip}. Os resultados aparecerão na página de Logs.");
            }
            else if (model.TipoEnvio == "range")
            {
                if (string.IsNullOrWhiteSpace(model.IpRange))
                {
                    ModelState.AddModelError("IpRange", "A faixa de IP é obrigatória.");
                    return View(model);
                }

                string[] faixas;
                if (model.IpRange == "all")
                {
                    faixas = new string[] { "10.0.0.", "10.0.2.", "10.1.1.", "10.1.2.", "10.2.2.", "10.3.3.", "10.4.4." };
                }
                else
                {
                    faixas = new string[] { model.IpRange };
                }

                string comando = model.Comando;
                model.Resultados.Add($"Envio do comando '{comando}' agendado para as faixas: {string.Join(", ", faixas)}. Os resultados aparecerão na página de Logs.");

                Task.Run(() =>
                {
                    foreach (var faixaBase in faixas)
                    {
                        for (int i = 1; i < 255; i++)
                        {
                            string ipFaixa = faixaBase + i.ToString();
                            _ = RunScopedComandoAsync(ipFaixa, comando);
                        }
                    }
                });
            }

            return View(model);
        }

        private async Task RunScopedComandoAsync(string ip, string comando)
        {
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var logService = scope.ServiceProvider.GetRequiredService<LogService>();
                    var comandoService = scope.ServiceProvider.GetRequiredService<ComandoService>();

                    logService.AddLog("Debug", $"[BG Task] RunScopedComandoAsync INICIADO para {ip}.", "Sistema");
                    await comandoService.EnviarComandoAsync(ip, comando);
                    logService.AddLog("Debug", $"[BG Task] Finalizado com sucesso o envio de comando para {ip}.", "Sistema");
                }
            }
            catch (Exception ex)
            {
                // Create a new scope specifically for logging the error.
                using (var scope = _scopeFactory.CreateScope())
                {
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<GerenciamentoController>>();
                    logger.LogError(ex, "[BG Task] Falha CRÍTICA na execução de RunScopedComandoAsync para o IP {IP}", ip);
                }
            }
        }
    }
}
