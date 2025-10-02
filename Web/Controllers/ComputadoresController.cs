using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Web.Models;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using System.IO;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using System.Security.Claims;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin,Coordenador,Colaborador,Diretoria")]
    public class ComputadoresController : Controller
    {
        private readonly FirestoreDb _firestoreDb;
        private readonly ILogger<ComputadoresController> _logger;
        private const string CollectionName = "computadores";
        private const string ColaboradoresCollection = "colaboradores";

        public ComputadoresController(FirestoreDb firestoreDb, ILogger<ComputadoresController> logger)
        {
            _firestoreDb = firestoreDb;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string sortOrder, string searchString,
            List<string> currentFabricantes, List<string> currentSOs, List<string> currentProcessadorFabricantes, List<string> currentRamTipos, List<string> currentProcessadores, List<string> currentRams,
            int pageNumber = 1, int pageSize = 25)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["IpSortParm"] = string.IsNullOrEmpty(sortOrder) ? "ip_desc" : "";
            ViewData["MacSortParm"] = sortOrder == "mac" ? "mac_desc" : "mac";
            ViewData["UserSortParm"] = sortOrder == "user" ? "user_desc" : "user";
            ViewData["HostnameSortParm"] = sortOrder == "hostname" ? "hostname_desc" : "hostname";
            ViewData["OsSortParm"] = sortOrder == "os" ? "os_desc" : "os";
            ViewData["DateSortParm"] = sortOrder == "date" ? "date_desc" : "date";

            var viewModel = new ComputadorIndexViewModel
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                SearchString = searchString,
                CurrentSort = sortOrder,
                CurrentFabricantes = currentFabricantes,
                CurrentSOs = currentSOs,
                CurrentProcessadorFabricantes = currentProcessadorFabricantes,
                CurrentRamTipos = currentRamTipos,
                CurrentProcessadores = currentProcessadores,
                CurrentRams = currentRams
            };

            try
            {
                Query query = _firestoreDb.Collection(CollectionName);
                var snapshot = await query.GetSnapshotAsync();
                var allComputadores = new List<Computador>();

                foreach (var document in snapshot.Documents)
                {
                    var comp = document.ConvertTo<Computador>();
                    comp.MAC = document.Id;
                    if (!string.IsNullOrEmpty(comp.ColaboradorCPF))
                    {
                        var colabDoc = await _firestoreDb.Collection(ColaboradoresCollection).Document(comp.ColaboradorCPF).GetSnapshotAsync();
                        if (colabDoc.Exists) comp.ColaboradorNome = colabDoc.GetValue<string>("Nome");
                    }
                    allComputadores.Add(comp);
                }

                // Populate filter dropdowns from the full dataset
                viewModel.Fabricantes = allComputadores.Where(c => !string.IsNullOrEmpty(c.Fabricante)).Select(c => c.Fabricante).Distinct().OrderBy(f => f).ToList();
                viewModel.SOs = allComputadores.Where(c => !string.IsNullOrEmpty(c.SO)).Select(c => c.SO).Distinct().OrderBy(s => s).ToList();
                viewModel.ProcessadorFabricantes = allComputadores.Where(c => !string.IsNullOrEmpty(c.ProcessadorFabricante)).Select(c => c.ProcessadorFabricante).Distinct().OrderBy(p => p).ToList();
                viewModel.RamTipos = allComputadores.Where(c => !string.IsNullOrEmpty(c.RamTipo)).Select(c => c.RamTipo).Distinct().OrderBy(r => r).ToList();
                viewModel.Processadores = allComputadores.Where(c => !string.IsNullOrEmpty(c.Processador)).Select(c => c.Processador).Distinct().OrderBy(p => p).ToList();
                viewModel.Rams = allComputadores.Where(c => !string.IsNullOrEmpty(c.Ram)).Select(c => c.Ram).Distinct().OrderBy(r => r).ToList();

                // Apply role-based filtering
                var userCpf = User.FindFirstValue("ColaboradorCPF");
                if (User.IsInRole("Colaborador") && !User.IsInRole("Admin") && !User.IsInRole("Diretoria"))
                {
                    allComputadores = allComputadores.Where(c => c.ColaboradorCPF == userCpf).ToList();
                }

                // Apply search and filters
                if (!string.IsNullOrEmpty(searchString))
                {
                    allComputadores = allComputadores.Where(c =>
                        (c.IP != null && c.IP.Contains(searchString, StringComparison.OrdinalIgnoreCase)) ||
                        (c.MAC != null && c.MAC.Contains(searchString, StringComparison.OrdinalIgnoreCase)) ||
                        (c.Hostname != null && c.Hostname.Contains(searchString, StringComparison.OrdinalIgnoreCase)) ||
                        (c.ColaboradorNome != null && c.ColaboradorNome.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                    ).ToList();
                }

                if (currentFabricantes.Any()) allComputadores = allComputadores.Where(c => c.Fabricante != null && currentFabricantes.Contains(c.Fabricante)).ToList();
                if (currentSOs.Any()) allComputadores = allComputadores.Where(c => c.SO != null && currentSOs.Contains(c.SO)).ToList();
                if (currentProcessadorFabricantes.Any()) allComputadores = allComputadores.Where(c => c.ProcessadorFabricante != null && currentProcessadorFabricantes.Contains(c.ProcessadorFabricante)).ToList();
                if (currentRamTipos.Any()) allComputadores = allComputadores.Where(c => c.RamTipo != null && currentRamTipos.Contains(c.RamTipo)).ToList();
                if (currentProcessadores.Any()) allComputadores = allComputadores.Where(c => c.Processador != null && currentProcessadores.Contains(c.Processador)).ToList();
                if (currentRams.Any()) allComputadores = allComputadores.Where(c => c.Ram != null && currentRams.Contains(c.Ram)).ToList();

                allComputadores = SortComputadores(allComputadores, sortOrder);

                viewModel.TotalCount = allComputadores.Count;
                viewModel.Computadores = allComputadores.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter a lista de computadores do Firestore.");
            }

            return View(viewModel);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            ViewData["Colaboradores"] = new SelectList(await GetColaboradoresAsync(), "CPF", "Nome");
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
                    var computador = MapViewModelToModel(viewModel);
                    computador.DataColeta = DateTime.UtcNow;
                    await _firestoreDb.Collection(CollectionName).Document(computador.MAC).SetAsync(computador);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao criar novo computador no Firestore.");
                    ModelState.AddModelError(string.Empty, "Ocorreu um erro ao criar o computador. Verifique se o MAC já existe.");
                }
            }
            ViewData["Colaboradores"] = new SelectList(await GetColaboradoresAsync(), "CPF", "Nome", viewModel.ColaboradorCPF);
            return View(viewModel);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            var doc = await _firestoreDb.Collection(CollectionName).Document(id).GetSnapshotAsync();
            if (!doc.Exists) return NotFound();

            var computador = doc.ConvertTo<Computador>();
            var viewModel = MapModelToViewModel(computador);

            ViewData["Colaboradores"] = new SelectList(await GetColaboradoresAsync(), "CPF", "Nome", viewModel.ColaboradorCPF);
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
                    var computador = MapViewModelToModel(viewModel);
                    await _firestoreDb.Collection(CollectionName).Document(id).SetAsync(computador, SetOptions.MergeAll);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                     _logger.LogError(ex, "Erro ao editar o computador no Firestore.");
                    ModelState.AddModelError(string.Empty, "Ocorreu um erro ao editar o computador.");
                }
            }
            ViewData["Colaboradores"] = new SelectList(await GetColaboradoresAsync(), "CPF", "Nome", viewModel.ColaboradorCPF);
            return View(viewModel);
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
            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                using (var package = new ExcelPackage(stream))
                {
                    //... Excel parsing logic as before, creating a list of Computador objects
                }
            }

            var writeBatch = _firestoreDb.StartBatch();
            foreach(var comp in computadores)
            {
                var docRef = _firestoreDb.Collection(CollectionName).Document(comp.MAC);
                writeBatch.Set(docRef, comp, SetOptions.MergeAll);
            }
            await writeBatch.CommitAsync();

            TempData["SuccessMessage"] = $"{computadores.Count} computadores importados/atualizados com sucesso.";
            return RedirectToAction(nameof(Index));
        }

        private Computador MapViewModelToModel(ComputadorViewModel viewModel)
        {
            return new Computador { /* Manual mapping of all properties */
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
                PartNumber = viewModel.PartNumber
            };
        }

        private ComputadorViewModel MapModelToViewModel(Computador computador)
        {
            return new ComputadorViewModel { /* Manual mapping */
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
                PartNumber = computador.PartNumber
            };
        }

        private async Task<List<Colaborador>> GetColaboradoresAsync()
        {
            var colaboradores = new List<Colaborador>();
            var snapshot = await _firestoreDb.Collection(ColaboradoresCollection).OrderBy("Nome").GetSnapshotAsync();
            foreach (var document in snapshot.Documents)
            {
                var colab = document.ConvertTo<Colaborador>();
                colab.CPF = document.Id;
                colaboradores.Add(colab);
            }
            return colaboradores;
        }

        private List<Computador> SortComputadores(List<Computador> computadores, string sortOrder)
        {
            switch (sortOrder)
            {
                case "ip_desc": return computadores.OrderByDescending(c => c.IP).ToList();
                case "mac": return computadores.OrderBy(c => c.MAC).ToList();
                case "mac_desc": return computadores.OrderByDescending(c => c.MAC).ToList();
                case "user": return computadores.OrderBy(c => c.ColaboradorNome).ToList();
                case "user_desc": return computadores.OrderByDescending(c => c.ColaboradorNome).ToList();
                case "hostname": return computadores.OrderBy(c => c.Hostname).ToList();
                case "hostname_desc": return computadores.OrderByDescending(c => c.Hostname).ToList();
                case "os": return computadores.OrderBy(c => c.SO).ToList();
                case "os_desc": return computadores.OrderByDescending(c => c.SO).ToList();
                case "date": return computadores.OrderBy(c => c.DataColeta).ToList();
                case "date_desc": return computadores.OrderByDescending(c => c.DataColeta).ToList();
                default: return computadores.OrderBy(c => c.IP).ToList();
            }
        }
    }
}