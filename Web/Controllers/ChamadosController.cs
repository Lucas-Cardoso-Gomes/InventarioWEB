using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Web.Models;
using Google.Cloud.Firestore;
using System.Linq;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin,Coordenador,Colaborador,Diretoria")]
    public class ChamadosController : Controller
    {
        private readonly FirestoreDb _firestoreDb;
        private readonly ILogger<ChamadosController> _logger;
        private const string CollectionName = "chamados";
        private const string ColaboradoresCollection = "colaboradores";

        public ChamadosController(FirestoreDb firestoreDb, ILogger<ChamadosController> logger)
        {
            _firestoreDb = firestoreDb;
            _logger = logger;
        }

        public async Task<IActionResult> Index(List<string> statuses, List<string> selectedAdmins)
        {
            if (statuses == null || !statuses.Any())
            {
                statuses = new List<string> { "Aberto", "Em Andamento" };
            }
            ViewBag.SelectedStatuses = statuses;
            ViewBag.Admins = await GetAdminsFromChamadosAsync();
            ViewBag.SelectedAdmins = selectedAdmins;

            var chamados = new List<Chamado>();
            var userCpf = User.FindFirstValue("ColaboradorCPF");

            try
            {
                Query query = _firestoreDb.Collection(CollectionName);
                var snapshot = await query.GetSnapshotAsync();
                var allChamados = new List<Chamado>();

                foreach (var document in snapshot.Documents)
                {
                    var chamado = document.ConvertTo<Chamado>();
                    chamado.ID = document.Id;
                    allChamados.Add(chamado);
                }

                // Role-based filtering
                if (User.IsInRole("Colaborador") && !User.IsInRole("Admin") && !User.IsInRole("Diretoria"))
                {
                    allChamados = allChamados.Where(c => c.ColaboradorCPF == userCpf).ToList();
                }
                // (Add more complex role logic for Coordenador if needed)

                // Status and Admin filtering
                if (statuses.Any())
                {
                    allChamados = allChamados.Where(c => statuses.Contains(c.Status)).ToList();
                }
                if (selectedAdmins != null && selectedAdmins.Any())
                {
                    allChamados = allChamados.Where(c => c.AdminCPF != null && selectedAdmins.Contains(c.AdminCPF)).ToList();
                }

                // Enrich with names
                foreach (var chamado in allChamados)
                {
                    if (!string.IsNullOrEmpty(chamado.ColaboradorCPF))
                        chamado.ColaboradorNome = await GetColaboradorName(chamado.ColaboradorCPF);
                    if (!string.IsNullOrEmpty(chamado.AdminCPF))
                        chamado.AdminNome = await GetColaboradorName(chamado.AdminCPF);
                }

                chamados = allChamados;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter a lista de chamados do Firestore.");
            }

            return View(chamados);
        }

        [Authorize(Roles = "Admin,Coordenador")]
        public async Task<IActionResult> Dashboard(DateTime? startDate, DateTime? endDate)
        {
            var viewModel = new ChamadoDashboardViewModel();
            try
            {
                Query query = _firestoreDb.Collection(CollectionName);
                if (startDate.HasValue) query = query.WhereGreaterThanOrEqualTo("DataCriacao", startDate.Value);
                if (endDate.HasValue) query = query.WhereLessThanOrEqualTo("DataCriacao", endDate.Value.AddDays(1));

                var snapshot = await query.GetSnapshotAsync();
                var chamados = snapshot.Documents.Select(doc => doc.ConvertTo<Chamado>()).ToList();

                viewModel.TotalChamados = chamados.Count;
                viewModel.Top10Servicos = chamados.GroupBy(c => c.Servico)
                                                  .Select(g => new ChartData { Label = g.Key, Value = g.Count() })
                                                  .OrderByDescending(x => x.Value).Take(10).ToList();

                // These require fetching collaborator names, which can be slow.
                // For a real app, denormalization would be better.
                var adminCpfCounts = chamados.Where(c => c.AdminCPF != null).GroupBy(c => c.AdminCPF)
                                             .ToDictionary(g => g.Key, g => g.Count());
                viewModel.TotalChamadosPorAdmin = new List<ChartData>();
                foreach(var item in adminCpfCounts)
                {
                    viewModel.TotalChamadosPorAdmin.Add(new ChartData { Label = await GetColaboradorName(item.Key) ?? item.Key, Value = item.Value });
                }

                var colabCpfCounts = chamados.GroupBy(c => c.ColaboradorCPF)
                                             .ToDictionary(g => g.Key, g => g.Count());
                viewModel.Top10Colaboradores = new List<ChartData>();
                foreach(var item in colabCpfCounts.OrderByDescending(x => x.Value).Take(10))
                {
                     viewModel.Top10Colaboradores.Add(new ChartData { Label = await GetColaboradorName(item.Key) ?? item.Key, Value = item.Value });
                }
            }
            catch (Exception ex)
            {
                 _logger.LogError(ex, "Erro ao gerar dashboard de chamados do Firestore.");
            }

            return View(viewModel);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Colaboradores = new SelectList(await GetColaboradoresAsync(), "CPF", "Nome");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Chamado chamado)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    chamado.AdminCPF = User.FindFirstValue("ColaboradorCPF");
                    chamado.DataCriacao = DateTime.UtcNow;
                    await _firestoreDb.Collection(CollectionName).AddAsync(chamado);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao criar chamado no Firestore.");
                }
            }
            ViewBag.Colaboradores = new SelectList(await GetColaboradoresAsync(), "CPF", "Nome", chamado.ColaboradorCPF);
            return View(chamado);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            var doc = await _firestoreDb.Collection(CollectionName).Document(id).GetSnapshotAsync();
            if (!doc.Exists) return NotFound();

            var chamado = doc.ConvertTo<Chamado>();
            chamado.ID = doc.Id;
            ViewBag.Colaboradores = new SelectList(await GetColaboradoresAsync(), "CPF", "Nome", chamado.ColaboradorCPF);
            return View(chamado);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string id, Chamado chamado)
        {
            if (id != chamado.ID) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var docRef = _firestoreDb.Collection(CollectionName).Document(id);
                    chamado.DataAlteracao = DateTime.UtcNow;
                    chamado.AdminCPF = User.FindFirstValue("ColaboradorCPF");
                    await docRef.SetAsync(chamado, SetOptions.MergeAll);
                    return RedirectToAction(nameof(Index));
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex, "Erro ao editar chamado no Firestore.");
                }
            }
            ViewBag.Colaboradores = new SelectList(await GetColaboradoresAsync(), "CPF", "Nome", chamado.ColaboradorCPF);
            return View(chamado);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
             if (string.IsNullOrEmpty(id)) return NotFound();
            var doc = await _firestoreDb.Collection(CollectionName).Document(id).GetSnapshotAsync();
            if (!doc.Exists) return NotFound();

            var chamado = doc.ConvertTo<Chamado>();
            chamado.ID = doc.Id;
            return View(chamado);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            try
            {
                await _firestoreDb.Collection(CollectionName).Document(id).DeleteAsync();
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Erro ao excluir chamado do Firestore.");
            }
            return RedirectToAction(nameof(Index));
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

        private async Task<List<Colaborador>> GetAdminsFromChamadosAsync()
        {
            var adminCpfs = new HashSet<string>();
            var snapshot = await _firestoreDb.Collection(CollectionName).Select("AdminCPF").GetSnapshotAsync();
            foreach(var doc in snapshot.Documents)
            {
                if(doc.TryGetValue("AdminCPF", out string cpf) && !string.IsNullOrEmpty(cpf))
                {
                    adminCpfs.Add(cpf);
                }
            }

            var admins = new List<Colaborador>();
            foreach(var cpf in adminCpfs)
            {
                var adminDoc = await _firestoreDb.Collection(ColaboradoresCollection).Document(cpf).GetSnapshotAsync();
                if(adminDoc.Exists)
                {
                    var admin = adminDoc.ConvertTo<Colaborador>();
                    admin.CPF = adminDoc.Id;
                    admins.Add(admin);
                }
            }
            return admins.OrderBy(a => a.Nome).ToList();
        }

        private async Task<string> GetColaboradorName(string cpf)
        {
            if (string.IsNullOrEmpty(cpf)) return null;
            var doc = await _firestoreDb.Collection(ColaboradoresCollection).Document(cpf).GetSnapshotAsync();
            return doc.Exists ? doc.GetValue<string>("Nome") : null;
        }
    }
}