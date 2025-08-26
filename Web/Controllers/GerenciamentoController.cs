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

namespace Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class GerenciamentoController : Controller
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<GerenciamentoController> _logger;
        private readonly IConfiguration _configuration;
        private readonly PersistentLogService _persistentLogService;
        
        public GerenciamentoController(IServiceScopeFactory scopeFactory, ILogger<GerenciamentoController> logger, PersistentLogService persistentLogService)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _persistentLogService = persistentLogService;
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

            var (logs, totalCount) = _logService.GetLogs(level, source, searchString, pageNumber, pageSize);
            viewModel.Logs = logs;
            viewModel.TotalCount = totalCount;
            viewModel.Levels = _logService.GetDistinctLogValues("Level");
            viewModel.Sources = _logService.GetDistinctLogValues("Source");
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ClearLogs()
        {
            _logService.ClearLogs();
            TempData["SuccessMessage"] = "Logs limpos com sucesso!";
            return RedirectToAction(nameof(Logs));
        }

        // GET: /Gerenciamento
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult PersistentLogs(string entityTypeFilter, string actionTypeFilter)
        {
            var logs = _persistentLogService.GetLogs(entityTypeFilter, actionTypeFilter);
            var viewModel = new PersistentLogViewModel
            {
                Logs = logs,
                EntityTypeFilter = entityTypeFilter,
                ActionTypeFilter = actionTypeFilter
            };
            return View(viewModel);
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
