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
    public class ComputadoresController : Controller
    {
        private readonly IComputadorService _computadorService;
        private readonly ILogger<ComputadoresController> _logger;
        private readonly PersistentLogService _persistentLogService;
        private readonly IHistoricoTrocasService _historicoTrocasService;
        private readonly ManutencaoService _manutencaoService;

        public ComputadoresController(
            IComputadorService computadorService,
            ILogger<ComputadoresController> logger,
            PersistentLogService persistentLogService,
            IHistoricoTrocasService historicoTrocasService,
            ManutencaoService manutencaoService)
        {
            _computadorService = computadorService;
            _logger = logger;
            _persistentLogService = persistentLogService;
            _historicoTrocasService = historicoTrocasService;
            _manutencaoService = manutencaoService;
        }

        public IActionResult Index(string sortOrder, string searchString,
            List<string> currentFabricantes, List<string> currentSOs, List<string> currentProcessadorFabricantes,
            List<string> currentRamTipos, List<string> currentProcessadores, List<string> currentRams,
            int pageNumber = 1, int pageSize = 25)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["IpSortParm"] = string.IsNullOrEmpty(sortOrder) ? "ip_desc" : "";
            ViewData["MacSortParm"] = sortOrder == "mac" ? "mac_desc" : "mac";
            ViewData["UserSortParm"] = sortOrder == "user" ? "user_desc" : "user";
            ViewData["HostnameSortParm"] = sortOrder == "hostname" ? "hostname_desc" : "hostname";
            ViewData["OsSortParm"] = sortOrder == "os" ? "os_desc" : "os";
            ViewData["DateSortParm"] = sortOrder == "date" ? "date_desc" : "date";
            ViewData["CurrentFilter"] = searchString;

            try
            {
                var userCpf = User.FindFirstValue("ColaboradorCPF");
                bool isColaboradorOnly = User.IsInRole("Colaborador") && !User.IsInRole("Admin") && !User.IsInRole("Diretoria");
                bool isCoordenadorOnly = User.IsInRole("Coordenador") && !User.IsInRole("Admin") && !User.IsInRole("Diretoria");

                var viewModel = _computadorService.GetPagedComputadores(sortOrder, searchString,
                    currentFabricantes, currentSOs, currentProcessadorFabricantes,
                    currentRamTipos, currentProcessadores, currentRams,
                    userCpf, isColaboradorOnly, isCoordenadorOnly,
                    pageNumber, pageSize);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter a lista de computadores.");
                ViewBag.Message = "Ocorreu um erro ao obter a lista de computadores. Por favor, tente novamente mais tarde.";
                return View(new ComputadorIndexViewModel());
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

            var computadores = new List<Computador>();
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
                            var sanitizedCpf = SanitizeCpf(worksheet.Cells[row, 3].Value?.ToString().Trim());
                            var computador = new Computador
                            {
                                MAC = worksheet.Cells[row, 1].Value?.ToString().Trim(),
                                IP = worksheet.Cells[row, 2].Value?.ToString().Trim(),
                                ColaboradorCPF = string.IsNullOrEmpty(sanitizedCpf) ? null : sanitizedCpf,
                                Hostname = worksheet.Cells[row, 4].Value?.ToString().Trim(),
                                Fabricante = worksheet.Cells[row, 5].Value?.ToString().Trim(),
                                Processador = worksheet.Cells[row, 6].Value?.ToString().Trim(),
                                ProcessadorFabricante = worksheet.Cells[row, 7].Value?.ToString().Trim(),
                                ProcessadorCore = worksheet.Cells[row, 8].Value?.ToString().Trim(),
                                ProcessadorThread = worksheet.Cells[row, 9].Value?.ToString().Trim(),
                                ProcessadorClock = worksheet.Cells[row, 10].Value?.ToString().Trim(),
                                Ram = worksheet.Cells[row, 11].Value?.ToString().Trim(),
                                RamTipo = worksheet.Cells[row, 12].Value?.ToString().Trim(),
                                RamVelocidade = worksheet.Cells[row, 13].Value?.ToString().Trim(),
                                RamVoltagem = worksheet.Cells[row, 14].Value?.ToString().Trim(),
                                RamPorModule = worksheet.Cells[row, 15].Value?.ToString().Trim(),
                                ArmazenamentoC = worksheet.Cells[row, 16].Value?.ToString().Trim(),
                                ArmazenamentoCTotal = worksheet.Cells[row, 17].Value?.ToString().Trim(),
                                ArmazenamentoCLivre = worksheet.Cells[row, 18].Value?.ToString().Trim(),
                                ArmazenamentoD = worksheet.Cells[row, 19].Value?.ToString().Trim(),
                                ArmazenamentoDTotal = worksheet.Cells[row, 20].Value?.ToString().Trim(),
                                ArmazenamentoDLivre = worksheet.Cells[row, 21].Value?.ToString().Trim(),
                                ConsumoCPU = worksheet.Cells[row, 22].Value?.ToString().Trim(),
                                SO = worksheet.Cells[row, 23].Value?.ToString().Trim(),
                                PartNumber = worksheet.Cells[row, 24].Value?.ToString().Trim()
                            };

                            if (!string.IsNullOrWhiteSpace(computador.MAC))
                            {
                                computadores.Add(computador);
                            }
                        }
                    }
                }

                var (adicionados, atualizados, invalidCpfs) = _computadorService.ImportList(computadores);
                TempData["SuccessMessage"] = $"{adicionados} computadores adicionados e {atualizados} atualizados com sucesso.";
                if (invalidCpfs.Any())
                {
                    TempData["WarningMessage"] = $"Os seguintes computadores (MAC) foram importados, mas o CPF do colaborador não foi encontrado: {string.Join(", ", invalidCpfs)}";
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
            ViewData["Colaboradores"] = new SelectList(_computadorService.GetColaboradores(), "CPF", "Nome");
            return View(new ComputadorViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(ComputadorViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var comp = new Computador
                    {
                        MAC = viewModel.MAC,
                        IP = viewModel.IP,
                        ColaboradorCPF = viewModel.ColaboradorCPF,
                        Hostname = viewModel.Hostname,
                        Fabricante = viewModel.Fabricante,
                        Processador = viewModel.Processador,
                        ProcessadorFabricante = viewModel.ProcessadorFabricante,
                        ProcessadorCore = viewModel.ProcessadorCore,
                        ProcessadorThread = viewModel.ProcessadorThread,
                        ProcessadorClock = viewModel.ProcessadorClock,
                        Ram = viewModel.Ram,
                        RamTipo = viewModel.RamTipo,
                        RamVelocidade = viewModel.RamVelocidade,
                        RamVoltagem = viewModel.RamVoltagem,
                        RamPorModule = viewModel.RamPorModule,
                        ArmazenamentoC = viewModel.ArmazenamentoC,
                        ArmazenamentoCTotal = viewModel.ArmazenamentoCTotal,
                        ArmazenamentoCLivre = viewModel.ArmazenamentoCLivre,
                        ArmazenamentoD = viewModel.ArmazenamentoD,
                        ArmazenamentoDTotal = viewModel.ArmazenamentoDTotal,
                        ArmazenamentoDLivre = viewModel.ArmazenamentoDLivre,
                        ConsumoCPU = viewModel.ConsumoCPU,
                        SO = viewModel.SO,
                        PartNumber = viewModel.PartNumber,
                        DataGarantia = viewModel.DataGarantia,
                        BateriaWearLevel = viewModel.BateriaWearLevel,
                        TempoAtividade = viewModel.TempoAtividade,
                        Localizacao = viewModel.Localizacao,
                        Backup = viewModel.Backup,
                        ProcessadorTemperatura = viewModel.ProcessadorTemperatura
                    };

                    _computadorService.Create(comp);

                    await _persistentLogService.LogChangeAsync("Computador", "Create", User.Identity.Name, null, viewModel);

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao criar um novo computador.");
                    ModelState.AddModelError(string.Empty, "Ocorreu um erro ao criar o computador. Verifique se o MAC já existe.");
                }
            }
            ViewData["Colaboradores"] = new SelectList(_computadorService.GetColaboradores(), "CPF", "Nome", viewModel.ColaboradorCPF);
            return View(viewModel);
        }

        public IActionResult Details(string id)
        {
            if (id == null) return NotFound();
            Computador computador = _computadorService.FindById(id);
            if (computador == null) return NotFound();

            var viewModel = new ComputadorDetailsViewModel
            {
                Computador = computador,
                HistoricoManutencoes = _manutencaoService.GetManutencoesByEquipamento("Computador", id),
                HistoricoTrocas = _historicoTrocasService.GetHistoricoByEquipamento("Computador", id)
            };

            return View(viewModel);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Edit(string id)
        {
            if (id == null) return NotFound();
            Computador computador = _computadorService.FindById(id);
            if (computador == null) return NotFound();

            var viewModel = new ComputadorViewModel
            {
                MAC = computador.MAC,
                IP = computador.IP,
                ColaboradorCPF = computador.ColaboradorCPF,
                Hostname = computador.Hostname,
                Fabricante = computador.Fabricante,
                Processador = computador.Processador,
                ProcessadorFabricante = computador.ProcessadorFabricante,
                ProcessadorCore = computador.ProcessadorCore,
                ProcessadorThread = computador.ProcessadorThread,
                ProcessadorClock = computador.ProcessadorClock,
                Ram = computador.Ram,
                RamTipo = computador.RamTipo,
                RamVelocidade = computador.RamVelocidade,
                RamVoltagem = computador.RamVoltagem,
                RamPorModule = computador.RamPorModule,
                ArmazenamentoC = computador.ArmazenamentoC,
                ArmazenamentoCTotal = computador.ArmazenamentoCTotal,
                ArmazenamentoCLivre = computador.ArmazenamentoCLivre,
                ArmazenamentoD = computador.ArmazenamentoD,
                ArmazenamentoDTotal = computador.ArmazenamentoDTotal,
                ArmazenamentoDLivre = computador.ArmazenamentoDLivre,
                ConsumoCPU = computador.ConsumoCPU,
                SO = computador.SO,
                PartNumber = computador.PartNumber,
                DataGarantia = computador.DataGarantia,
                BateriaWearLevel = computador.BateriaWearLevel,
                TempoAtividade = computador.TempoAtividade,
                Localizacao = computador.Localizacao,
                Backup = computador.Backup,
                ProcessadorTemperatura = computador.ProcessadorTemperatura
            };
            ViewData["Colaboradores"] = new SelectList(_computadorService.GetColaboradores(), "CPF", "Nome", viewModel.ColaboradorCPF);
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string id, ComputadorViewModel viewModel)
        {
            if (id != viewModel.MAC) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var oldComputador = _computadorService.FindById(id);

                    var comp = new Computador
                    {
                        MAC = viewModel.MAC,
                        IP = viewModel.IP,
                        ColaboradorCPF = viewModel.ColaboradorCPF,
                        Hostname = viewModel.Hostname,
                        Fabricante = viewModel.Fabricante,
                        Processador = viewModel.Processador,
                        ProcessadorFabricante = viewModel.ProcessadorFabricante,
                        ProcessadorCore = viewModel.ProcessadorCore,
                        ProcessadorThread = viewModel.ProcessadorThread,
                        ProcessadorClock = viewModel.ProcessadorClock,
                        Ram = viewModel.Ram,
                        RamTipo = viewModel.RamTipo,
                        RamVelocidade = viewModel.RamVelocidade,
                        RamVoltagem = viewModel.RamVoltagem,
                        RamPorModule = viewModel.RamPorModule,
                        ArmazenamentoC = viewModel.ArmazenamentoC,
                        ArmazenamentoCTotal = viewModel.ArmazenamentoCTotal,
                        ArmazenamentoCLivre = viewModel.ArmazenamentoCLivre,
                        ArmazenamentoD = viewModel.ArmazenamentoD,
                        ArmazenamentoDTotal = viewModel.ArmazenamentoDTotal,
                        ArmazenamentoDLivre = viewModel.ArmazenamentoDLivre,
                        ConsumoCPU = viewModel.ConsumoCPU,
                        SO = viewModel.SO,
                        PartNumber = viewModel.PartNumber,
                        DataGarantia = viewModel.DataGarantia,
                        BateriaWearLevel = viewModel.BateriaWearLevel,
                        TempoAtividade = viewModel.TempoAtividade,
                        Localizacao = viewModel.Localizacao,
                        Backup = viewModel.Backup,
                        ProcessadorTemperatura = viewModel.ProcessadorTemperatura
                    };

                    _computadorService.Update(comp);

                    if (oldComputador != null)
                    {
                        var currentUser = User.Identity?.Name ?? "Sistema";
                        if (oldComputador.ColaboradorCPF != viewModel.ColaboradorCPF)
                            _historicoTrocasService.RegistrarAlteracao(id, "Computador", "Usuário", oldComputador.ColaboradorCPF, viewModel.ColaboradorCPF, currentUser);

                        if (oldComputador.Processador != viewModel.Processador)
                            _historicoTrocasService.RegistrarAlteracao(id, "Computador", "Processador", oldComputador.Processador, viewModel.Processador, currentUser);

                        if (oldComputador.Ram != viewModel.Ram)
                            _historicoTrocasService.RegistrarAlteracao(id, "Computador", "RAM", oldComputador.Ram, viewModel.Ram, currentUser);

                        if (oldComputador.ArmazenamentoCTotal != viewModel.ArmazenamentoCTotal)
                            _historicoTrocasService.RegistrarAlteracao(id, "Computador", "Armazenamento C", oldComputador.ArmazenamentoCTotal, viewModel.ArmazenamentoCTotal, currentUser);
                    }

                    await _persistentLogService.LogChangeAsync("Computador", "Update", User.Identity.Name, oldComputador, viewModel);

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao editar o computador.");
                    ModelState.AddModelError(string.Empty, "Ocorreu um erro ao editar o computador.");
                }
            }
            ViewData["Colaboradores"] = new SelectList(_computadorService.GetColaboradores(), "CPF", "Nome", viewModel.ColaboradorCPF);
            return View(viewModel);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Delete(string id)
        {
            if (id == null) return NotFound();
            Computador computador = _computadorService.FindById(id);
            if (computador == null) return NotFound();
            return View(computador);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            try
            {
                var computador = _computadorService.FindById(id);
                if (computador != null)
                {
                    _computadorService.Delete(id);
                    await _persistentLogService.LogChangeAsync("Computador", "Delete", User.Identity.Name, computador, null);
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao excluir o computador.");
                ViewBag.Message = "Ocorreu um erro ao excluir o computador. Por favor, tente novamente mais tarde.";
                Computador computador = _computadorService.FindById(id);
                if (computador == null)
                {
                    return NotFound();
                }
                return View(computador);
            }
        }

        private string SanitizeCpf(string cpf)
        {
            if (string.IsNullOrEmpty(cpf)) return cpf;
            return new string(cpf.Where(char.IsDigit).ToArray());
        }
    }
}
