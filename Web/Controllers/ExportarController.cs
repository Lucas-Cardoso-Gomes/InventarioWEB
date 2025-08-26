using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using Web.Models;
using Microsoft.AspNetCore.Authorization;
using Web.Services;
using System.Threading.Tasks;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin,Coordenador")]
    public class ExportarController : Controller
    {
        private readonly UserService _userService;
        private readonly ComputadorService _computadorService;
        private readonly MonitorService _monitorService;
        private readonly PerifericoService _perifericoService;
        private readonly ILogger<ExportarController> _logger;

        public ExportarController(IConfiguration configuration, ILogger<ExportarController> logger, UserService userService, ComputadorService computadorService, MonitorService monitorService, PerifericoService perifericoService)
        {
            _logger = logger;
            _userService = userService;
            _computadorService = computadorService;
            _monitorService = monitorService;
            _perifericoService = perifericoService;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new ExportarViewModel
            {
                // Computer filters
                Fabricantes = await _computadorService.GetDistinctComputerValuesAsync("Fabricante"),
                SOs = await _computadorService.GetDistinctComputerValuesAsync("SO"),
                ProcessadorFabricantes = await _computadorService.GetDistinctComputerValuesAsync("ProcessadorFabricante"),
                RamTipos = await _computadorService.GetDistinctComputerValuesAsync("RamTipo"),
                Processadores = await _computadorService.GetDistinctComputerValuesAsync("Processador"),
                Rams = await _computadorService.GetDistinctComputerValuesAsync("Ram"),

                // Monitor filters
                Marcas = await _monitorService.GetDistinctMonitorValuesAsync("Marca"),
                Tamanhos = await _monitorService.GetDistinctMonitorValuesAsync("Tamanho"),
                Modelos = await _monitorService.GetDistinctMonitorValuesAsync("Modelo"),

                // Periferico filters
                TiposPeriferico = await _perifericoService.GetDistinctPerifericoValuesAsync("Tipo"),

                // User filter
                Colaboradores = (await _userService.GetAllUsersAsync()).Select(c => c.Nome).ToList()
            };
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Export(ExportarViewModel viewModel)
        {
            var csvBuilder = new StringBuilder();
            string fileName = $"export_{DateTime.Now:yyyyMMddHHmmss}.csv";

            if (viewModel.ExportMode == ExportMode.PorDispositivo)
            {
                fileName = $"export_{viewModel.DeviceType}_{DateTime.Now:yyyyMMddHHmmss}.csv";
                switch (viewModel.DeviceType)
                {
                    case DeviceType.Computadores:
                        var (computadores, _) = await _computadorService.GetComputadoresAsync(User, null, null, viewModel.CurrentFabricantes, viewModel.CurrentSOs, viewModel.CurrentProcessadorFabricantes, viewModel.CurrentRamTipos, viewModel.CurrentProcessadores, viewModel.CurrentRams, 1, int.MaxValue);
                        csvBuilder.AppendLine("MAC,IP,Usuario,Hostname,Fabricante,Processador,SO,DataColeta");
                        foreach (var c in computadores)
                        {
                            csvBuilder.AppendLine($"{c.MAC},{c.IP},{c.User?.Nome},{c.Hostname},{c.Fabricante},{c.Processador},{c.SO},{c.DataColeta}");
                        }
                        break;
                    case DeviceType.Monitores:
                        var (monitores, _) = await _monitorService.GetMonitoresAsync(User, null, viewModel.CurrentMarcas, viewModel.CurrentTamanhos, viewModel.CurrentModelos, 1, int.MaxValue);
                        csvBuilder.AppendLine("PartNumber,Usuario,Marca,Modelo,Tamanho");
                        foreach (var m in monitores)
                        {
                            csvBuilder.AppendLine($"{m.PartNumber},{m.User?.Nome},{m.Marca},{m.Modelo},{m.Tamanho}");
                        }
                        break;
                    case DeviceType.Perifericos:
                        var (perifericos, _) = await _perifericoService.GetPerifericosAsync(User, null, 1, int.MaxValue);
                        csvBuilder.AppendLine("ID,Usuario,Tipo,DataEntrega,PartNumber");
                        foreach (var p in perifericos)
                        {
                            csvBuilder.AppendLine($"{p.ID},{p.User?.Nome},{p.Tipo},{p.DataEntrega},{p.PartNumber}");
                        }
                        break;
                }
            }
            else if (viewModel.ExportMode == ExportMode.PorColaborador)
            {
                var user = (await _userService.GetAllUsersAsync()).FirstOrDefault(u => u.Nome == viewModel.ColaboradorNome);
                if (user != null)
                {
                    fileName = $"export_colaborador_{viewModel.ColaboradorNome}_{DateTime.Now:yyyyMMddHHmmss}.csv";

                    var (computadores, _) = await _computadorService.GetComputadoresAsync(User, null, null, null, null, null, null, null, null, 1, int.MaxValue);
                    computadores = computadores.Where(c => c.UserId == user.Id).ToList();
                    csvBuilder.AppendLine("Computadores");
                    csvBuilder.AppendLine("MAC,IP,Hostname,Fabricante,Processador,SO,DataColeta");
                    foreach (var c in computadores)
                    {
                        csvBuilder.AppendLine($"{c.MAC},{c.IP},{c.Hostname},{c.Fabricante},{c.Processador},{c.SO},{c.DataColeta}");
                    }

                    var (monitores, _) = await _monitorService.GetMonitoresAsync(User, null, null, null, null, 1, int.MaxValue);
                    monitores = monitores.Where(m => m.UserId == user.Id).ToList();
                    csvBuilder.AppendLine();
                    csvBuilder.AppendLine("Monitores");
                    csvBuilder.AppendLine("PartNumber,Usuario,Marca,Modelo,Tamanho");
                    foreach (var m in monitores)
                    {
                        csvBuilder.AppendLine($"{m.PartNumber},{m.User?.Nome},{m.Marca},{m.Modelo},{m.Tamanho}");
                    }

                    var (perifericos, _) = await _perifericoService.GetPerifericosAsync(User, null, 1, int.MaxValue);
                    perifericos = perifericos.Where(p => p.UserId == user.Id).ToList();
                    csvBuilder.AppendLine();
                    csvBuilder.AppendLine("Perifericos");
                    csvBuilder.AppendLine("ID,Usuario,Tipo,DataEntrega,PartNumber");
                    foreach (var p in perifericos)
                    {
                        csvBuilder.AppendLine($"{p.ID},{p.User?.Nome},{p.Tipo},{p.DataEntrega},{p.PartNumber}");
                    }
                }
            }

            return File(Encoding.UTF8.GetBytes(csvBuilder.ToString()), "text/csv", fileName);
        }
    }
}
