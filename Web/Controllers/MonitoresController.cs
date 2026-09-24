using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using Web.Models;
using Web.Services;
using Monitor = Web.Models.Monitor;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin,Coordenador,Colaborador,Diretoria")]
    public class MonitoresController : Controller
    {
        private readonly IMonitorService _monitorService;
        private readonly ILogger<MonitoresController> _logger;
        private readonly PersistentLogService _persistentLogService;
        private readonly IHistoricoTrocasService _historicoTrocasService;
        private readonly ManutencaoService _manutencaoService;

        public MonitoresController(
            IMonitorService monitorService,
            ILogger<MonitoresController> logger,
            PersistentLogService persistentLogService,
            IHistoricoTrocasService historicoTrocasService,
            ManutencaoService manutencaoService)
        {
            _monitorService = monitorService;
            _logger = logger;
            _persistentLogService = persistentLogService;
            _historicoTrocasService = historicoTrocasService;
            _manutencaoService = manutencaoService;
        }

        public IActionResult Index(List<string> currentMarcas, List<string> currentTamanhos, List<string> currentModelos)
        {
            try
            {
                var userCpf = User.FindFirstValue("ColaboradorCPF");
                bool isColaboradorOnly = User.IsInRole("Colaborador") && !User.IsInRole("Admin") && !User.IsInRole("Diretoria");
                bool isCoordenadorOnly = User.IsInRole("Coordenador") && !User.IsInRole("Admin") && !User.IsInRole("Diretoria");

                var viewModel = _monitorService.GetFilteredMonitores(currentMarcas, currentTamanhos, currentModelos, userCpf, isColaboradorOnly, isCoordenadorOnly);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter a lista de monitores.");
                return View(new MonitorIndexViewModel());
            }
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

            var monitores = new List<Monitor>();
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
                            var sanitizedCpf = SanitizeCpf(worksheet.Cells[row, 2].Value?.ToString().Trim());
                            var monitor = new Monitor
                            {
                                PartNumber = worksheet.Cells[row, 1].Value?.ToString().Trim(),
                                ColaboradorCPF = string.IsNullOrEmpty(sanitizedCpf) ? null : sanitizedCpf,
                                Marca = worksheet.Cells[row, 3].Value?.ToString().Trim(),
                                Modelo = worksheet.Cells[row, 4].Value?.ToString().Trim(),
                                Tamanho = worksheet.Cells[row, 5].Value?.ToString().Trim()
                            };

                            if (!string.IsNullOrWhiteSpace(monitor.PartNumber))
                            {
                                monitores.Add(monitor);
                            }
                        }
                    }
                }

                var (adicionados, atualizados, invalidCpfs) = _monitorService.ImportList(monitores);
                TempData["SuccessMessage"] = $"{adicionados} monitores adicionados e {atualizados} atualizados com sucesso.";
                if (invalidCpfs.Any())
                {
                    TempData["WarningMessage"] = $"Os seguintes monitores (PartNumber) foram importados, mas o CPF do colaborador não foi encontrado: {string.Join(", ", invalidCpfs)}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao importar o arquivo Excel.");
                TempData["ErrorMessage"] = "Ocorreu um erro durante a importação do arquivo. Verifique se o formato está correto.";
            }

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewData["Colaboradores"] = new SelectList(_monitorService.GetColaboradores(), "CPF", "Nome");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Monitor monitor)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _monitorService.Create(monitor);
                    await _persistentLogService.LogChangeAsync("Monitor", "Create", User.Identity.Name, null, monitor);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao criar monitor.");
                    ModelState.AddModelError(string.Empty, "Ocorreu um erro ao criar o monitor. Verifique se o PartNumber já existe.");
                }
            }
            ViewData["Colaboradores"] = new SelectList(_monitorService.GetColaboradores(), "CPF", "Nome", monitor.ColaboradorCPF);
            return View(monitor);
        }

        public IActionResult Details(string id)
        {
            if (id == null) return NotFound();
            Monitor monitor = _monitorService.FindById(id);
            if (monitor == null) return NotFound();

            var viewModel = new MonitorDetailsViewModel
            {
                Monitor = monitor,
                HistoricoManutencoes = _manutencaoService.GetManutencoesByEquipamento("Monitor", id),
                HistoricoTrocas = _historicoTrocasService.GetHistoricoByEquipamento("Monitor", id)
            };

            return View(viewModel);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Edit(string id)
        {
            if (id == null) return NotFound();
            Monitor monitor = _monitorService.FindById(id);
            if (monitor == null) return NotFound();
            ViewData["Colaboradores"] = new SelectList(_monitorService.GetColaboradores(), "CPF", "Nome", monitor.ColaboradorCPF);
            return View(monitor);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string id, Monitor monitor)
        {
            if (id != monitor.PartNumber) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var oldMonitor = _monitorService.FindById(id);
                    _monitorService.Update(monitor);

                    if (oldMonitor != null)
                    {
                        var currentUser = User.Identity?.Name ?? "Sistema";
                        if (oldMonitor.ColaboradorCPF != monitor.ColaboradorCPF)
                            _historicoTrocasService.RegistrarAlteracao(id, "Monitor", "Usuário", oldMonitor.ColaboradorCPF, monitor.ColaboradorCPF, currentUser);
                    }

                    await _persistentLogService.LogChangeAsync("Monitor", "Update", User.Identity.Name, oldMonitor, monitor);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao editar monitor.");
                    ModelState.AddModelError(string.Empty, "Ocorreu um erro ao editar o monitor.");
                }
            }
            ViewData["Colaboradores"] = new SelectList(_monitorService.GetColaboradores(), "CPF", "Nome", monitor.ColaboradorCPF);
            return View(monitor);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Delete(string id)
        {
            if (id == null) return NotFound();
            Monitor monitor = _monitorService.FindById(id);
            if (monitor == null) return NotFound();
            return View(monitor);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            try
            {
                var monitor = _monitorService.FindById(id);
                if (monitor != null)
                {
                    _monitorService.Delete(id);
                    await _persistentLogService.LogChangeAsync("Monitor", "Delete", User.Identity.Name, monitor, null);
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao excluir monitor.");
                ViewBag.ErrorMessage = "Ocorreu um erro ao excluir o monitor.";
                return View(_monitorService.FindById(id));
            }
        }

        private string SanitizeCpf(string cpf)
        {
            if (string.IsNullOrEmpty(cpf)) return cpf;
            return new string(cpf.Where(char.IsDigit).ToArray());
        }
    }
}
