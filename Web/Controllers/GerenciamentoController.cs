using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using web.Models;
using web.Services;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace Web.Controllers
{
    public class GerenciamentoController : Controller
    {
        private readonly ILogger<GerenciamentoController> _logger;
        private readonly IConfiguration _configuration;
        private readonly ColetaService _coletaService;

        public GerenciamentoController(ILogger<GerenciamentoController> logger, IConfiguration configuration, ColetaService coletaService)
        {
            _logger = logger;
            _configuration = configuration;
            _coletaService = coletaService;
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
                // Don't wait, run in background
                Task.Run(() => _coletaService.ColetarDadosAsync(model.IpAddress, (result) => _logger.LogInformation(result)));
                model.Resultados.Add($"Coleta iniciada para o IP: {model.IpAddress}");
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

                model.Resultados.Add($"Varredura iniciada para as faixas: {string.Join(", ", faixas)}");

                Task.Run(() =>
                {
                    foreach (var faixaBase in faixas)
                    {
                        Parallel.For(1, 256, i =>
                        {
                            string ipFaixa = faixaBase + i.ToString();
                            _coletaService.ColetarDadosAsync(ipFaixa, (result) => _logger.LogInformation(result)).Wait();
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
        public async Task<IActionResult> Comandos(ComandoViewModel model)
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
                string result = await _coletaService.EnviarComandoAsync(model.IpAddress, model.Comando);
                model.Resultados.Add(result);
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

                model.Resultados.Add($"Envio do comando '{model.Comando}' iniciado para as faixas: {string.Join(", ", faixas)}");

                // We don't wait for this to finish
                Task.Run(() =>
                {
                    foreach (var faixaBase in faixas)
                    {
                        Parallel.For(1, 256, i =>
                        {
                            string ipFaixa = faixaBase + i.ToString();
                            _coletaService.EnviarComandoAsync(ipFaixa, model.Comando)
                                .ContinueWith(t => _logger.LogInformation(t.Result));
                        });
                    }
                });
            }

            return View(model);
        }
    }
}
