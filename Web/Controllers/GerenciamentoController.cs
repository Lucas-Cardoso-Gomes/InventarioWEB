using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using web.Models;
using Web.Services;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Web.Controllers
{
    public class GerenciamentoController : Controller
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<GerenciamentoController> _logger;
        private readonly IConfiguration _configuration;
        
        public GerenciamentoController(IServiceProvider serviceProvider, ILogger<GerenciamentoController> logger, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;
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
                using (var connection = new System.Data.SqlClient.SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
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
                    using (var countCommand = new System.Data.SqlClient.SqlCommand(countSql, connection))
                    {
                        foreach (var p in parameters) countCommand.Parameters.AddWithValue(p.Key, p.Value);
                        viewModel.TotalCount = (int)countCommand.ExecuteScalar();
                    }

                    // Get paginated logs
                    string sql = $"SELECT Id, Timestamp, Level, Message, Source FROM Logs {whereSql} ORDER BY Timestamp DESC OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";
                    using (var command = new System.Data.SqlClient.SqlCommand(sql, connection))
                    {
                        foreach (var p in parameters) command.Parameters.AddWithValue(p.Key, p.Value);
                        command.Parameters.AddWithValue("@offset", (pageNumber - 1) * pageSize);
                        command.Parameters.AddWithValue("@pageSize", pageSize);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                viewModel.Logs.Add(new Log
                                {
                                    Id = reader.GetInt32(0),
                                    Timestamp = reader.GetDateTime(1),
                                    Level = reader.GetString(2),
                                    Message = reader.GetString(3),
                                    Source = reader.IsDBNull(4) ? null : reader.GetString(4)
                                });
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
                using (var connection = new System.Data.SqlClient.SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    connection.Open();
                    string sql = "TRUNCATE TABLE Logs";
                    using (var command = new System.Data.SqlClient.SqlCommand(sql, connection))
                    {
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

        private List<string> GetDistinctLogValues(System.Data.SqlClient.SqlConnection connection, string columnName)
        {
            var values = new List<string>();
            string sql = $"SELECT DISTINCT {columnName} FROM Logs WHERE {columnName} IS NOT NULL ORDER BY {columnName}";
            using (var command = new System.Data.SqlClient.SqlCommand(sql, connection))
            {
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
            using (var scope = _serviceProvider.CreateScope())
            {
                var coletaService = scope.ServiceProvider.GetRequiredService<ColetaService>();
                var logService = scope.ServiceProvider.GetRequiredService<LogService>();
                try
                {
                    await coletaService.ColetarDadosAsync(ip, (result) => {
                        _logger.LogInformation(result);
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
            // I will put the entire method in a try-catch to log any unexpected errors.
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var logService = scope.ServiceProvider.GetRequiredService<LogService>();
                    logService.AddLog("Critical", "--- LOG INICIAL ---", "DIAGNOSTICO");
                    logService.AddLog("Debug", $"Ação Comandos recebida. Tipo: {model.TipoEnvio}, IP: {model.IpAddress}, Comando: {model.Comando}", "DIAGNOSTICO");

                    model.ComandoIniciado = true;
                    logService.AddLog("Critical", "--- PASSO 1: ComandoIniciado = true ---", "DIAGNOSTICO");

                    if (!ModelState.IsValid)
                    {
                        logService.AddLog("Critical", "--- FALHA: ModelState is INVALID ---", "DIAGNOSTICO");
                        return View(model);
                    }
                    logService.AddLog("Critical", "--- PASSO 2: ModelState is VALID ---", "DIAGNOSTICO");

                    if (model.TipoEnvio == "ip")
                    {
                        logService.AddLog("Critical", "--- PASSO 3: Entrou no IF para TipoEnvio 'ip' ---", "DIAGNOSTICO");
                        if (string.IsNullOrWhiteSpace(model.IpAddress))
                        {
                            logService.AddLog("Critical", "--- FALHA: IP está vazio ---", "DIAGNOSTICO");
                            ModelState.AddModelError("IpAddress", "O endereço IP é obrigatório.");
                            return View(model);
                        }

                        logService.AddLog("Critical", "--- PASSO 4: IP verificado ---", "DIAGNOSTICO");
                        string ip = model.IpAddress;
                        string comando = model.Comando;

                        logService.AddLog("Critical", "--- PASSO 5: Preparando para agendar a tarefa ---", "DIAGNOSTICO");
                        Task.Run(() => RunScopedComandoAsync(ip, comando));
                        logService.AddLog("Critical", "--- PASSO 6: Tarefa agendada com Task.Run ---", "DIAGNOSTICO");

                        model.Resultados.Add($"Envio do comando '{comando}' agendado para o IP: {ip}.");
                    }
                    else
                    {
                        logService.AddLog("Critical", $"--- FALHA: TipoEnvio não é 'ip'. É '{model.TipoEnvio}' ---", "DIAGNOSTICO");
                    }

                    logService.AddLog("Critical", "--- PASSO 7: Fim do método, retornando a View ---", "DIAGNOSTICO");
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                // Fallback logging if everything else fails.
                using (var scope = _serviceProvider.CreateScope())
                {
                    var logService = scope.ServiceProvider.GetRequiredService<LogService>();
                    logService.AddLog("Critical", $"--- ERRO INESPERADO NO MÉTODO 'Comandos': {ex.ToString()} ---", "DIAGNOSTICO");
                }
                // Still return the view so the user doesn't see a server error page.
                return View(model);
            }
        }

        private async Task RunScopedComandoAsync(string ip, string comando)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var logService = scope.ServiceProvider.GetRequiredService<LogService>();
                    logService.AddLog("Debug", $"[BG Task] RunScopedComandoAsync INICIADO para {ip}.", "Sistema");

                    try
                    {
                        logService.AddLog("Debug", $"[BG Task] Criado escopo para enviar comando '{comando}' para {ip}.", "Sistema");
                        
                        var comandoService = scope.ServiceProvider.GetRequiredService<ComandoService>();
                        logService.AddLog("Debug", $"[BG Task] ComandoService resolvido para {ip}. Chamando EnviarComandoAsync...", "Sistema");

                        await comandoService.EnviarComandoAsync(ip, comando);
                        
                        logService.AddLog("Debug", $"[BG Task] Finalizado com sucesso o envio de comando para {ip}.", "Sistema");
                    }
                    catch (Exception ex)
                    {
                        // Loga a exceção que ocorreu dentro da tarefa de fundo.
                        logService.AddLog("Error", $"[BG Task] Falha na tarefa de envio de comando para {ip}: {ex.GetBaseException().Message}", "Sistema");
                    }
                }
            }
            catch (Exception ex)
            {
                // Fallback extremo: se até a criação do escopo falhar, logue no console do servidor web.
                _logger.LogError(ex, "[BG Task] Falha CRÍTICA ao criar escopo de serviço para RunScopedComandoAsync.");
            }
        }
    }
}
