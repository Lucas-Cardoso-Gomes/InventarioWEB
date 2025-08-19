using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using web.Models;
using Web.Services; // Corrigido
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace Web.Controllers
{
    public class GerenciamentoController : Controller
    {
        private readonly ILogger<GerenciamentoController> _logger;
        private readonly IConfiguration _configuration;
        private readonly ColetaService _coletaService;
        private readonly LogService _logService;

        public GerenciamentoController(ILogger<GerenciamentoController> logger, IConfiguration configuration, ColetaService coletaService, LogService logService)
        {
            _logger = logger;
            _configuration = configuration;
            _coletaService = coletaService;
            _logService = logService;
        }

        // GET: /Gerenciamento/Logs
        public IActionResult Logs()
        {
            var logs = new List<Log>();
            try
            {
                using (var connection = new System.Data.SqlClient.SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    connection.Open();
                    var sql = "SELECT Id, Timestamp, Level, Message, Source FROM Logs ORDER BY Timestamp DESC";
                    using (var command = new System.Data.SqlClient.SqlCommand(sql, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                logs.Add(new Log
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
                // Adiciona um log de erro à lista para ser exibido na tela
                logs.Add(new Log
                {
                    Timestamp = DateTime.Now,
                    Level = "Error",
                    Message = "Não foi possível carregar os logs. Verifique a conexão com o banco de dados e se a tabela 'Logs' existe.",
                    Source = "Sistema"
                });
            }
            return View(logs);
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

            Action<string> onResult = (result) =>
            {
                // Este callback é executado em um thread de fundo.
                // Apenas logamos aqui, não tentamos atualizar a UI diretamente.
                _logger.LogInformation(result);
            };

            if (model.TipoColeta == "ip")
            {
                if (string.IsNullOrWhiteSpace(model.IpAddress))
                {
                    ModelState.AddModelError("IpAddress", "O endereço IP é obrigatório.");
                    return View(model);
                }
                // Don't wait, run in background
                Task.Run(() => _coletaService.ColetarDadosAsync(model.IpAddress, onResult));
                model.Resultados.Add($"Coleta agendada para o IP: {model.IpAddress}. Os resultados aparecerão na página de Logs.");
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
                            // Não usamos .Wait() aqui para permitir paralelismo total
                            _coletaService.ColetarDadosAsync(ipFaixa, onResult);
                        });
                    }
                });
            }

            return View(model);
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
            model.ComandoIniciado = true;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.TipoEnvio == "ip")
            {
                if (string.IsNullOrWhiteSpace(model.IpAddress))
                {
                    ModelState.AddModelError("IpAddress", "O endereço IP é obrigatório.");
                    return View(model);
                }

                // Don't wait, run in background
                Task.Run(() => _coletaService.EnviarComandoAsync(model.IpAddress, model.Comando))
                    .ContinueWith(t => {
                        if (t.IsFaulted) {
                            _logService.AddLog("Error", $"Falha na tarefa de envio de comando para {model.IpAddress}: {t.Exception.GetBaseException().Message}", "Sistema");
                        }
                    }, TaskContinuationOptions.OnlyOnFaulted);

                model.Resultados.Add($"Envio do comando '{model.Comando}' agendado para o IP: {model.IpAddress}. Os resultados aparecerão na página de Logs.");
            }
            else if (model.TipoEnvio == "range")
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

                model.Resultados.Add($"Envio do comando '{model.Comando}' agendado para as faixas: {string.Join(", ", faixas)}. Os resultados aparecerão na página de Logs.");

                // We don't wait for this to finish
                Task.Run(() =>
                {
                    foreach (var faixaBase in faixas)
                    {
                        Parallel.For(1, 256, i =>
                        {
                            string ipFaixa = faixaBase + i.ToString();
                            _coletaService.EnviarComandoAsync(ipFaixa, model.Comando)
                                .ContinueWith(t => {
                                    if (t.IsFaulted) {
                                        _logService.AddLog("Error", $"Falha na tarefa de envio de comando para {ipFaixa}: {t.Exception.GetBaseException().Message}", "Sistema");
                                    }
                                }, TaskContinuationOptions.OnlyOnFaulted);
                        });
                    }
                });
            }

            return View(model);
        }
    }
}
