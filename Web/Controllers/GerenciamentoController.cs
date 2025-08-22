using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Web.Models;
using Web.Services;
using System.Threading.Tasks;
using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Web.Data;
using Microsoft.EntityFrameworkCore;

namespace Web.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class GerenciamentoController : Controller
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<GerenciamentoController> _logger;
        private readonly ApplicationDbContext _context;

        public GerenciamentoController(IServiceScopeFactory scopeFactory, ILogger<GerenciamentoController> logger, ApplicationDbContext context)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _context = context;
        }

        // GET: /Gerenciamento/Logs
        public async Task<IActionResult> Logs(string level, string source, string searchString, int pageNumber = 1, int pageSize = 25)
        {
            var query = _context.Logs.AsQueryable();

            if (!string.IsNullOrEmpty(level))
            {
                query = query.Where(l => l.Level == level);
            }
            if (!string.IsNullOrEmpty(source))
            {
                query = query.Where(l => l.Source == source);
            }
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(l => l.Message.Contains(searchString));
            }

            var paginatedLogs = await PaginatedList<Log>.CreateAsync(query.OrderByDescending(l => l.Timestamp), pageNumber, pageSize);

            var viewModel = new LogViewModel
            {
                Logs = paginatedLogs,
                Levels = await _context.Logs.Select(l => l.Level).Distinct().ToListAsync(),
                Sources = await _context.Logs.Select(l => l.Source).Distinct().ToListAsync(),
                CurrentLevel = level,
                CurrentSource = source,
                SearchString = searchString,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = await query.CountAsync()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearLogs()
        {
            try
            {
                await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE Logs");
                TempData["SuccessMessage"] = "Logs limpos com sucesso!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao limpar os logs.");
                TempData["ErrorMessage"] = "Ocorreu um erro ao limpar os logs.";
            }
            return RedirectToAction(nameof(Logs));
        }

        public IActionResult Index() => View();

        public IActionResult Coletar() => View(new ColetaViewModel());

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
                Task.Run(() => RunScopedColetaAsync(model.IpAddress));
                model.Resultados.Add($"Coleta agendada para o IP: {model.IpAddress}.");
            }
            else if (model.TipoColeta == "range")
            {
                string[] faixas = (model.IpRange == "all")
                    ? new[] { "10.0.0.", "10.0.2.", "10.1.1.", "10.1.2.", "10.2.2.", "10.3.3.", "10.4.4." }
                    : new[] { model.IpRange };

                model.Resultados.Add($"Varredura de coleta agendada para as faixas: {string.Join(", ", faixas)}.");
                Task.Run(() =>
                {
                    foreach (var faixaBase in faixas)
                    {
                        Parallel.For(1, 255, i => RunScopedColetaAsync(faixaBase + i.ToString()));
                    }
                });
            }
            return View(model);
        }

        private async Task RunScopedColetaAsync(string ip)
        {
            using var scope = _scopeFactory.CreateScope();
            var coletaService = scope.ServiceProvider.GetRequiredService<ColetaService>();
            var logService = scope.ServiceProvider.GetRequiredService<LogService>();
            try
            {
                await coletaService.ColetarDadosAsync(ip, async (result) => {
                    await logService.AddLogAsync("Info", result, "Coleta");
                });
            }
            catch (Exception ex)
            {
                await logService.AddLogAsync("Error", $"Falha na tarefa de coleta para {ip}: {ex.Message}", "Sistema");
            }
        }

        public IActionResult Comandos() => View(new ComandoViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Comandos(ComandoViewModel model)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var logService = scope.ServiceProvider.GetRequiredService<LogService>();
                await logService.AddLogAsync("Debug", $"Ação Comandos recebida. Tipo: {model.TipoEnvio}, IP: {model.IpAddress}, Range: {model.IpRange}, Comando: {model.Comando}", "Sistema");
            }
            
            model.ComandoIniciado = true;
            if (model.TipoEnvio == "ip")
            {
                 if (string.IsNullOrWhiteSpace(model.IpAddress))
                {
                    ModelState.AddModelError("IpAddress", "O endereço IP é obrigatório.");
                    return View(model);
                }
                Task.Run(() => RunScopedComandoAsync(model.IpAddress, model.Comando));
                model.Resultados.Add($"Envio do comando '{model.Comando}' agendado para o IP: {model.IpAddress}.");
            }
            else if (model.TipoEnvio == "range")
            {
                string[] faixas = (model.IpRange == "all")
                    ? new[] { "10.0.0.", "10.0.2.", "10.1.1.", "10.1.2.", "10.2.2.", "10.3.3.", "10.4.4." }
                    : new[] { model.IpRange };

                model.Resultados.Add($"Envio do comando '{model.Comando}' agendado para as faixas: {string.Join(", ", faixas)}.");
                Task.Run(() =>
                {
                    foreach (var faixaBase in faixas)
                    {
                        Parallel.For(1, 255, i => RunScopedComandoAsync(faixaBase + i.ToString(), model.Comando));
                    }
                });
            }
            return View(model);
        }

        private async Task RunScopedComandoAsync(string ip, string comando)
        {
            using var scope = _scopeFactory.CreateScope();
            var logService = scope.ServiceProvider.GetRequiredService<LogService>();
            var comandoService = scope.ServiceProvider.GetRequiredService<ComandoService>();
            try
            {
                await logService.AddLogAsync("Debug", $"[BG Task] RunScopedComandoAsync INICIADO para {ip}.", "Sistema");
                await comandoService.EnviarComandoAsync(ip, comando);
                await logService.AddLogAsync("Debug", $"[BG Task] Finalizado com sucesso o envio de comando para {ip}.", "Sistema");
            }
            catch (Exception ex)
            {
                await logService.AddLogAsync("Error", $"[BG Task] Falha CRÍTICA na execução de RunScopedComandoAsync para o IP {ip}: {ex.Message}", "Sistema");
            }
        }
    }
}
