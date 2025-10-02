using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Web.Models;
using System.Security.Claims;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using System.Linq;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin,Coordenador,Colaborador,Diretoria")]
    public class PerifericosController : Controller
    {
        private readonly FirestoreDb _firestoreDb;
        private readonly ILogger<PerifericosController> _logger;
        private const string CollectionName = "perifericos";
        private const string ColaboradoresCollection = "colaboradores";

        public PerifericosController(FirestoreDb firestoreDb, ILogger<PerifericosController> logger)
        {
            _firestoreDb = firestoreDb;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string searchString)
        {
            ViewData["CurrentFilter"] = searchString;
            var perifericos = new List<Periferico>();
            try
            {
                Query query = _firestoreDb.Collection(CollectionName);
                var snapshot = await query.GetSnapshotAsync();
                var allPerifericos = new List<Periferico>();

                foreach (var doc in snapshot.Documents)
                {
                    var periferico = doc.ConvertTo<Periferico>();
                    periferico.PartNumber = doc.Id;
                    if (!string.IsNullOrEmpty(periferico.ColaboradorCPF))
                    {
                        var colabDoc = await _firestoreDb.Collection(ColaboradoresCollection).Document(periferico.ColaboradorCPF).GetSnapshotAsync();
                        if (colabDoc.Exists) periferico.ColaboradorNome = colabDoc.GetValue<string>("Nome");
                    }
                    allPerifericos.Add(periferico);
                }

                if (!string.IsNullOrEmpty(searchString))
                {
                    allPerifericos = allPerifericos.Where(p =>
                        (p.ColaboradorNome != null && p.ColaboradorNome.Contains(searchString, StringComparison.OrdinalIgnoreCase)) ||
                        (p.Tipo != null && p.Tipo.Contains(searchString, StringComparison.OrdinalIgnoreCase)) ||
                        (p.PartNumber != null && p.PartNumber.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                    ).ToList();
                }

                perifericos = allPerifericos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter a lista de periféricos do Firestore.");
            }
            return View(perifericos);
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
        public async Task<IActionResult> Create(Periferico periferico)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _firestoreDb.Collection(CollectionName).Document(periferico.PartNumber).SetAsync(periferico);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao criar periférico no Firestore.");
                    ModelState.AddModelError("", "Erro ao criar periférico.");
                }
            }
            ViewData["Colaboradores"] = new SelectList(await GetColaboradoresAsync(), "CPF", "Nome", periferico.ColaboradorCPF);
            return View(periferico);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            var doc = await _firestoreDb.Collection(CollectionName).Document(id).GetSnapshotAsync();
            if (!doc.Exists) return NotFound();

            var periferico = doc.ConvertTo<Periferico>();
            periferico.PartNumber = doc.Id;

            ViewData["Colaboradores"] = new SelectList(await GetColaboradoresAsync(), "CPF", "Nome", periferico.ColaboradorCPF);
            return View(periferico);
        }

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
                    await _firestoreDb.Collection(CollectionName).Document(id).SetAsync(periferico, SetOptions.MergeAll);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                     _logger.LogError(ex, $"Erro ao editar periférico {id} no Firestore.");
                }
            }
            ViewData["Colaboradores"] = new SelectList(await GetColaboradoresAsync(), "CPF", "Nome", periferico.ColaboradorCPF);
            return View(periferico);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            var doc = await _firestoreDb.Collection(CollectionName).Document(id).GetSnapshotAsync();
            if (!doc.Exists) return NotFound();

            var periferico = doc.ConvertTo<Periferico>();
            periferico.PartNumber = doc.Id;
            return View(periferico);
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
            catch(Exception ex)
            {
                _logger.LogError(ex, $"Erro ao excluir periférico {id} do Firestore.");
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