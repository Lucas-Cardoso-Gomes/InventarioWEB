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

namespace Web.Controllers
{
    [Authorize(Roles = "Admin,Coordenador,Colaborador,Diretoria")]
    public class PerifericosController : Controller
    {
        private readonly IPerifericoService _perifericoService;
        private readonly ILogger<PerifericosController> _logger;
        private readonly PersistentLogService _persistentLogService;
        private readonly IHistoricoTrocasService _historicoTrocasService;
        private readonly ManutencaoService _manutencaoService;

        public PerifericosController(
            IPerifericoService perifericoService,
            ILogger<PerifericosController> logger,
            PersistentLogService persistentLogService,
            IHistoricoTrocasService historicoTrocasService,
            ManutencaoService manutencaoService)
        {
            _perifericoService = perifericoService;
            _logger = logger;
            _persistentLogService = persistentLogService;
            _historicoTrocasService = historicoTrocasService;
            _manutencaoService = manutencaoService;
        }

        // GET: Perifericos
        public IActionResult Index(string searchString)
        {
            ViewData["CurrentFilter"] = searchString;
            try
            {
                var userCpf = User.FindFirstValue("ColaboradorCPF");
                bool isColaboradorOnly = User.IsInRole("Colaborador") && !User.IsInRole("Admin") && !User.IsInRole("Diretoria");
                bool isCoordenadorOnly = User.IsInRole("Coordenador") && !User.IsInRole("Admin") && !User.IsInRole("Diretoria");

                var perifericos = _perifericoService.GetFilteredPerifericos(searchString, userCpf, isColaboradorOnly, isCoordenadorOnly);
                return View(perifericos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter a lista de periféricos.");
                return View(new List<Periferico>());
            }
        }

        // GET: Perifericos/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewData["Colaboradores"] = new SelectList(_perifericoService.GetColaboradores(), "CPF", "Nome");
            return View();
        }

        // POST: Perifericos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Periferico periferico)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _perifericoService.Create(periferico);
                    await _persistentLogService.LogChangeAsync("Periferico", "Create", User.Identity.Name, null, periferico);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao criar periférico.");
                    ModelState.AddModelError(string.Empty, "Ocorreu um erro ao criar o periférico.");
                }
            }
            ViewData["Colaboradores"] = new SelectList(_perifericoService.GetColaboradores(), "CPF", "Nome", periferico.ColaboradorCPF);
            return View(periferico);
        }

        public IActionResult Details(string id)
        {
            if (id == null) return NotFound();
            Periferico periferico = _perifericoService.FindById(id);
            if (periferico == null) return NotFound();

            var viewModel = new PerifericoDetailsViewModel
            {
                Periferico = periferico,
                HistoricoManutencoes = _manutencaoService.GetManutencoesByEquipamento("Periferico", id),
                HistoricoTrocas = _historicoTrocasService.GetHistoricoByEquipamento("Periferico", id)
            };

            return View(viewModel);
        }

        // GET: Perifericos/Edit/5
        [Authorize(Roles = "Admin")]
        public IActionResult Edit(string id)
        {
            if (id == null) return NotFound();
            Periferico periferico = _perifericoService.FindById(id);
            if (periferico == null) return NotFound();
            ViewData["Colaboradores"] = new SelectList(_perifericoService.GetColaboradores(), "CPF", "Nome", periferico.ColaboradorCPF);
            return View(periferico);
        }

        // POST: Perifericos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string id, Periferico periferico)
        {
            if (id != periferico.PartNumber) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var oldPeriferico = _perifericoService.FindById(id);
                    _perifericoService.Update(periferico);

                    if (oldPeriferico != null)
                    {
                        var currentUser = User.Identity?.Name ?? "Sistema";
                        if (oldPeriferico.ColaboradorCPF != periferico.ColaboradorCPF)
                            _historicoTrocasService.RegistrarAlteracao(id, "Periferico", "Usuário", oldPeriferico.ColaboradorCPF, periferico.ColaboradorCPF, currentUser);
                    }

                    await _persistentLogService.LogChangeAsync("Periferico", "Update", User.Identity.Name, oldPeriferico, periferico);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao editar periférico.");
                    ModelState.AddModelError(string.Empty, "Ocorreu um erro ao editar o periférico.");
                }
            }
            ViewData["Colaboradores"] = new SelectList(_perifericoService.GetColaboradores(), "CPF", "Nome", periferico.ColaboradorCPF);
            return View(periferico);
        }

        // GET: Perifericos/Delete/5
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(string id)
        {
            if (id == null) return NotFound();
            Periferico periferico = _perifericoService.FindById(id);
            if (periferico == null) return NotFound();
            return View(periferico);
        }

        // POST: Perifericos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            try
            {
                var periferico = _perifericoService.FindById(id);
                if (periferico != null)
                {
                    _perifericoService.Delete(id);
                    await _persistentLogService.LogChangeAsync("Periferico", "Delete", User.Identity.Name, periferico, null);
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao excluir periférico.");
                ViewBag.ErrorMessage = "Ocorreu um erro ao excluir o periférico.";
                return View(_perifericoService.FindById(id));
            }
        }

        private string SanitizeCpf(string cpf)
        {
            if (string.IsNullOrEmpty(cpf)) return cpf;
            return new string(cpf.Where(char.IsDigit).ToArray());
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

            var perifericos = new List<Periferico>();
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

                            DateTime? dataEntrega = null;
                            if (DateTime.TryParse(worksheet.Cells[row, 4].Value?.ToString()?.Trim(), out DateTime parsedDate))
                            {
                                dataEntrega = parsedDate;
                            }

                            var periferico = new Periferico
                            {
                                PartNumber = worksheet.Cells[row, 1].Value?.ToString().Trim(),
                                ColaboradorCPF = string.IsNullOrEmpty(sanitizedCpf) ? null : sanitizedCpf,
                                Tipo = worksheet.Cells[row, 3].Value?.ToString().Trim(),
                                DataEntrega = dataEntrega
                            };

                            if (!string.IsNullOrWhiteSpace(periferico.PartNumber))
                            {
                                perifericos.Add(periferico);
                            }
                        }
                    }
                }

                var (adicionados, atualizados, invalidCpfs) = _perifericoService.ImportList(perifericos);
                TempData["SuccessMessage"] = $"{adicionados} periféricos adicionados e {atualizados} atualizados com sucesso.";
                if (invalidCpfs.Any())
                {
                    TempData["WarningMessage"] = $"Os seguintes periféricos (PartNumber) foram importados, mas o CPF do colaborador não foi encontrado: {string.Join(", ", invalidCpfs)}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao importar o arquivo Excel.");
                TempData["ErrorMessage"] = "Ocorreu um erro durante a importação do arquivo. Verifique se o formato está correto.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
