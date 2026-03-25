using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Web.Models;
using Web.Services;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using System.IO;
using System.Threading.Tasks;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin,Coordenador")]
    public class ManutencoesController : Controller
    {
        private readonly ManutencaoService _manutencaoService;
        private readonly IDatabaseService _databaseService;
        private readonly PersistentLogService _persistentLogService;

        public ManutencoesController(ManutencaoService manutencaoService, PersistentLogService persistentLogService, IDatabaseService databaseService)
        {
            _manutencaoService = manutencaoService;
            _databaseService = databaseService;
            _persistentLogService = persistentLogService;
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

            var manutencoes = new List<Manutencao>();
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
                            DateTime? dataHardware = null;
                            if (DateTime.TryParse(worksheet.Cells[row, 4].Value?.ToString()?.Trim(), out DateTime parsedDataH))
                            {
                                dataHardware = parsedDataH;
                            }
                            
                            DateTime? dataSoftware = null;
                            if (DateTime.TryParse(worksheet.Cells[row, 5].Value?.ToString()?.Trim(), out DateTime parsedDataS))
                            {
                                dataSoftware = parsedDataS;
                            }
                            
                            DateTime? data = null;
                            if (DateTime.TryParse(worksheet.Cells[row, 7].Value?.ToString()?.Trim(), out DateTime parsedData))
                            {
                                data = parsedData;
                            }

                            var manutencao = new Manutencao
                            {
                                ComputadorMAC = worksheet.Cells[row, 1].Value?.ToString().Trim(),
                                MonitorPartNumber = worksheet.Cells[row, 2].Value?.ToString().Trim(),
                                PerifericoPartNumber = worksheet.Cells[row, 3].Value?.ToString().Trim(),
                                DataManutencaoHardware = dataHardware,
                                DataManutencaoSoftware = dataSoftware,
                                ManutencaoExterna = worksheet.Cells[row, 6].Value?.ToString().Trim(),
                                Data = data,
                                Historico = worksheet.Cells[row, 8].Value?.ToString().Trim()
                            };

                            // Basic validation: must have at least one device linked.
                            if (!string.IsNullOrWhiteSpace(manutencao.ComputadorMAC) || 
                                !string.IsNullOrWhiteSpace(manutencao.MonitorPartNumber) || 
                                !string.IsNullOrWhiteSpace(manutencao.PerifericoPartNumber))
                            {
                                manutencoes.Add(manutencao);
                            }
                        }
                    }
                }

                int adicionados = 0;

                foreach (var manutencao in manutencoes)
                {
                    // As IDs are auto-generated and there isn't a strict natural unique key combination, 
                    // we'll treat all imports as new records or you can adjust to find existing records if preferred.
                    _manutencaoService.AddManutencao(manutencao);
                    adicionados++;
                }

                TempData["SuccessMessage"] = $"{adicionados} manutenções adicionadas com sucesso.";
            }
            catch (Exception ex)
            {
                // In a real app we might log the exception.
                TempData["ErrorMessage"] = "Ocorreu um erro durante a importação do arquivo. Verifique se o formato está correto.";
            }

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Index(string partNumber, string colaborador, string hostname)
        {
            var manutencoes = _manutencaoService.GetAllManutencoes(partNumber, colaborador, hostname);

            var viewModel = new ManutencaoIndexViewModel
            {
                Manutencoes = manutencoes,
                PartNumber = partNumber,
                Colaborador = colaborador,
                Hostname = hostname
            };

            return View(viewModel);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewData["ComputadorMAC"] = new SelectList(GetComputadores().Select(c => new { Value = c.MAC, Text = $"{c.Hostname} ({c.MAC})" }), "Value", "Text");
            ViewData["MonitorPartNumber"] = new SelectList(GetMonitores().Select(m => new { Value = m.PartNumber, Text = $"{m.Modelo} ({m.PartNumber})" }), "Value", "Text");
            ViewData["PerifericoPartNumber"] = new SelectList(GetPerifericos().Select(p => new { Value = p.PartNumber, Text = $"{p.Tipo} ({p.PartNumber})" }), "Value", "Text");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Manutencao manutencao)
        {
            if (ModelState.IsValid)
            {
                _manutencaoService.AddManutencao(manutencao);

                await _persistentLogService.LogChangeAsync(
                    User.Identity.Name,
                    "CREATE",
                    "Manutencao",
                    $"Created maintenance record for device",
                    $"Computer: {manutencao.ComputadorMAC ?? "N/A"}, Monitor: {manutencao.MonitorPartNumber ?? "N/A"}, Periferico: {manutencao.PerifericoPartNumber ?? "N/A"}"
                );

                return RedirectToAction(nameof(Index));
            }
            ViewData["ComputadorMAC"] = new SelectList(GetComputadores().Select(c => new { Value = c.MAC, Text = $"{c.Hostname} ({c.MAC})" }), "Value", "Text", manutencao.ComputadorMAC);
            ViewData["MonitorPartNumber"] = new SelectList(GetMonitores().Select(m => new { Value = m.PartNumber, Text = $"{m.Modelo} ({m.PartNumber})" }), "Value", "Text", manutencao.MonitorPartNumber);
            ViewData["PerifericoPartNumber"] = new SelectList(GetPerifericos().Select(p => new { Value = p.PartNumber, Text = $"{p.Tipo} ({p.PartNumber})" }), "Value", "Text", manutencao.PerifericoPartNumber);
            return View(manutencao);
        }

        private List<Computador> GetComputadores()
        {
            var computadores = new List<Computador>();
            using (var connection = _databaseService.CreateConnection())
            {
                connection.Open();
                string sql = "SELECT MAC, Hostname FROM Computadores ORDER BY Hostname";
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            computadores.Add(new Computador
                            {
                                MAC = reader.GetString(0),
                                Hostname = reader.GetString(1)
                            });
                        }
                    }
                }
            }
            return computadores;
        }

        private List<Web.Models.Monitor> GetMonitores()
        {
            var monitores = new List<Web.Models.Monitor>();
            using (var connection = _databaseService.CreateConnection())
            {
                connection.Open();
                string sql = "SELECT PartNumber, Modelo FROM Monitores ORDER BY Modelo";
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            monitores.Add(new Web.Models.Monitor
                            {
                                PartNumber = reader.GetString(0),
                                Modelo = reader.GetString(1)
                            });
                        }
                    }
                }
            }
            return monitores;
        }

        private List<Periferico> GetPerifericos()
        {
            var perifericos = new List<Periferico>();
            using (var connection = _databaseService.CreateConnection())
            {
                connection.Open();
                string sql = "SELECT PartNumber, Tipo FROM Perifericos ORDER BY Tipo";
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            perifericos.Add(new Periferico
                            {
                                PartNumber = reader.GetString(0),
                                Tipo = reader.GetString(1)
                            });
                        }
                    }
                }
            }
            return perifericos;
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Edit(int id)
        {
            var manutencao = _manutencaoService.GetManutencaoById(id);
            if (manutencao == null)
            {
                return NotFound();
            }
            ViewData["ComputadorMAC"] = new SelectList(GetComputadores().Select(c => new { Value = c.MAC, Text = $"{c.Hostname} ({c.MAC})" }), "Value", "Text", manutencao.ComputadorMAC);
            ViewData["MonitorPartNumber"] = new SelectList(GetMonitores().Select(m => new { Value = m.PartNumber, Text = $"{m.Modelo} ({m.PartNumber})" }), "Value", "Text", manutencao.MonitorPartNumber);
            ViewData["PerifericoPartNumber"] = new SelectList(GetPerifericos().Select(p => new { Value = p.PartNumber, Text = $"{p.Tipo} ({p.PartNumber})" }), "Value", "Text", manutencao.PerifericoPartNumber);
            return View(manutencao);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, Manutencao manutencao)
        {
            if (id != manutencao.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                _manutencaoService.UpdateManutencao(manutencao);

                await _persistentLogService.LogChangeAsync(
                    User.Identity.Name,
                    "EDIT",
                    "Manutencao",
                    $"Updated maintenance record ID: {manutencao.Id}",
                    $"ID: {manutencao.Id}, Computer: {manutencao.ComputadorMAC ?? "N/A"}, Monitor: {manutencao.MonitorPartNumber ?? "N/A"}"
                );

                return RedirectToAction(nameof(Index));
            }
            ViewData["ComputadorMAC"] = new SelectList(GetComputadores().Select(c => new { Value = c.MAC, Text = $"{c.Hostname} ({c.MAC})" }), "Value", "Text", manutencao.ComputadorMAC);
            ViewData["MonitorPartNumber"] = new SelectList(GetMonitores().Select(m => new { Value = m.PartNumber, Text = $"{m.Modelo} ({m.PartNumber})" }), "Value", "Text", manutencao.MonitorPartNumber);
            ViewData["PerifericoPartNumber"] = new SelectList(GetPerifericos().Select(p => new { Value = p.PartNumber, Text = $"{p.Tipo} ({p.PartNumber})" }), "Value", "Text", manutencao.PerifericoPartNumber);
            return View(manutencao);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
            var manutencao = _manutencaoService.GetManutencaoById(id);
            if (manutencao == null)
            {
                return NotFound();
            }
            return View(manutencao);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var manutencao = _manutencaoService.GetManutencaoById(id);
            _manutencaoService.DeleteManutencao(id);

            if (manutencao != null)
            {
                await _persistentLogService.LogChangeAsync(
                    User.Identity.Name,
                    "DELETE",
                    "Manutencao",
                    $"Deleted maintenance record ID: {id}",
                    $"ID: {id}, Computer: {manutencao.ComputadorMAC ?? "N/A"}"
                );
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
