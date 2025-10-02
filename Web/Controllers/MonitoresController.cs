using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Web.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using System.IO;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using Monitor = Web.Models.Monitor;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin,Coordenador,Colaborador,Diretoria")]
    public class MonitoresController : Controller
    {
        private readonly FirestoreDb _firestoreDb;
        private readonly ILogger<MonitoresController> _logger;
        private const string CollectionName = "monitores";
        private const string ColaboradoresCollection = "colaboradores";

        public MonitoresController(FirestoreDb firestoreDb, ILogger<MonitoresController> logger)
        {
            _firestoreDb = firestoreDb;
            _logger = logger;
        }

        public async Task<IActionResult> Index(List<string> currentMarcas, List<string> currentTamanhos, List<string> currentModelos)
        {
            var viewModel = new MonitorIndexViewModel
            {
                CurrentMarcas = currentMarcas,
                CurrentTamanhos = currentTamanhos,
                CurrentModelos = currentModelos
            };

            try
            {
                Query query = _firestoreDb.Collection(CollectionName);
                var snapshot = await query.GetSnapshotAsync();
                var allMonitores = new List<Monitor>();

                foreach(var doc in snapshot.Documents)
                {
                    var monitor = doc.ConvertTo<Monitor>();
                    monitor.PartNumber = doc.Id;
                    if (!string.IsNullOrEmpty(monitor.ColaboradorCPF))
                    {
                        var colabDoc = await _firestoreDb.Collection(ColaboradoresCollection).Document(monitor.ColaboradorCPF).GetSnapshotAsync();
                        if (colabDoc.Exists) monitor.ColaboradorNome = colabDoc.GetValue<string>("Nome");
                    }
                    allMonitores.Add(monitor);
                }

                // Populate filters
                viewModel.Marcas = allMonitores.Where(m => !string.IsNullOrEmpty(m.Marca)).Select(m => m.Marca).Distinct().ToList();
                viewModel.Tamanhos = allMonitores.Where(m => !string.IsNullOrEmpty(m.Tamanho)).Select(m => m.Tamanho).Distinct().ToList();
                viewModel.Modelos = allMonitores.Where(m => !string.IsNullOrEmpty(m.Modelo)).Select(m => m.Modelo).Distinct().ToList();

                // Apply filters
                if (currentMarcas != null && currentMarcas.Any()) allMonitores = allMonitores.Where(m => currentMarcas.Contains(m.Marca)).ToList();
                if (currentTamanhos != null && currentTamanhos.Any()) allMonitores = allMonitores.Where(m => currentTamanhos.Contains(m.Tamanho)).ToList();
                if (currentModelos != null && currentModelos.Any()) allMonitores = allMonitores.Where(m => currentModelos.Contains(m.Modelo)).ToList();

                viewModel.Monitores = allMonitores;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter a lista de monitores do Firestore.");
            }
            return View(viewModel);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            ViewData["Colaboradores"] = new SelectList(await GetColaboradoresAsync(), "CPF", "Nome");
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
                    await _firestoreDb.Collection(CollectionName).Document(monitor.PartNumber).SetAsync(monitor);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao criar monitor no Firestore.");
                    ModelState.AddModelError("", "Erro ao criar monitor. Verifique se o PartNumber já existe.");
                }
            }
            ViewData["Colaboradores"] = new SelectList(await GetColaboradoresAsync(), "CPF", "Nome", monitor.ColaboradorCPF);
            return View(monitor);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            var doc = await _firestoreDb.Collection(CollectionName).Document(id).GetSnapshotAsync();
            if (!doc.Exists) return NotFound();

            var monitor = doc.ConvertTo<Monitor>();
            monitor.PartNumber = doc.Id;

            ViewData["Colaboradores"] = new SelectList(await GetColaboradoresAsync(), "CPF", "Nome", monitor.ColaboradorCPF);
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
                    await _firestoreDb.Collection(CollectionName).Document(id).SetAsync(monitor, SetOptions.MergeAll);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Erro ao editar monitor {id} no Firestore.");
                }
            }
            ViewData["Colaboradores"] = new SelectList(await GetColaboradoresAsync(), "CPF", "Nome", monitor.ColaboradorCPF);
            return View(monitor);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            var doc = await _firestoreDb.Collection(CollectionName).Document(id).GetSnapshotAsync();
            if (!doc.Exists) return NotFound();

            var monitor = doc.ConvertTo<Monitor>();
            monitor.PartNumber = doc.Id;
            return View(monitor);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            try
            {
                await _firestoreDb.Collection(CollectionName).Document(id).DeleteAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao excluir monitor {id} do Firestore.");
                return RedirectToAction(nameof(Index));
            }
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
    }
}