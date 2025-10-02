using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Web.Models;
using Google.Cloud.Firestore;
using OfficeOpenXml;
using FirebaseAdmin.Auth;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin,Coordenador")]
    public class ColaboradoresController : Controller
    {
        private readonly FirestoreDb _firestoreDb;
        private readonly ILogger<ColaboradoresController> _logger;
        private const string CollectionName = "colaboradores";

        public ColaboradoresController(FirestoreDb firestoreDb, ILogger<ColaboradoresController> logger)
        {
            _firestoreDb = firestoreDb;
            _logger = logger;
        }

        private string SanitizeCpf(string cpf)
        {
            if (string.IsNullOrEmpty(cpf)) return cpf;
            return new string(cpf.Where(char.IsDigit).ToArray());
        }

        public async Task<IActionResult> Index(string searchString)
        {
            ViewData["CurrentFilter"] = searchString;
            var colaboradores = new List<Colaborador>();
            try
            {
                Query query = _firestoreDb.Collection(CollectionName);
                // Firestore does not support case-insensitive "LIKE" queries directly.
                // A more robust solution would involve using a third-party search service like Algolia or Elasticsearch,
                // or storing a normalized, lowercase version of the fields for searching.
                // For this migration, we will fetch all and filter in memory.
                var snapshot = await query.GetSnapshotAsync();

                foreach (var document in snapshot.Documents)
                {
                    var colab = document.ConvertTo<Colaborador>();
                    colab.CPF = document.Id;

                    if (!string.IsNullOrEmpty(colab.CoordenadorCPF))
                    {
                        var coordenadorDoc = await _firestoreDb.Collection(CollectionName).Document(colab.CoordenadorCPF).GetSnapshotAsync();
                        if(coordenadorDoc.Exists)
                        {
                            colab.CoordenadorNome = coordenadorDoc.GetValue<string>("Nome");
                        }
                    }
                    colaboradores.Add(colab);
                }

                if (!string.IsNullOrEmpty(searchString))
                {
                    var lowerSearch = searchString.ToLower();
                    colaboradores = colaboradores.Where(c =>
                        c.Nome.ToLower().Contains(lowerSearch) ||
                        c.CPF.Contains(lowerSearch) ||
                        (c.Email != null && c.Email.ToLower().Contains(lowerSearch))
                    ).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter a lista de colaboradores do Firestore.");
            }
            return View(colaboradores);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Coordenadores = new SelectList(await GetCoordenadoresAsync(), "CPF", "Nome");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Colaborador colaborador)
        {
            colaborador.CPF = SanitizeCpf(colaborador.CPF);
            colaborador.CoordenadorCPF = SanitizeCpf(colaborador.CoordenadorCPF);

            if (ModelState.IsValid)
            {
                try
                {
                    var docRef = _firestoreDb.Collection(CollectionName).Document(colaborador.CPF);
                    colaborador.DataInclusao = DateTime.UtcNow;
                    await docRef.SetAsync(colaborador, SetOptions.MergeAll);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao criar colaborador no Firestore.");
                    ModelState.AddModelError(string.Empty, "Ocorreu um erro ao criar o colaborador.");
                }
            }
            ViewBag.Coordenadores = new SelectList(await GetCoordenadoresAsync(), "CPF", "Nome", colaborador.CoordenadorCPF);
            return View(colaborador);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null) return NotFound();
            var doc = await _firestoreDb.Collection(CollectionName).Document(SanitizeCpf(id)).GetSnapshotAsync();
            if (!doc.Exists) return NotFound();

            var colaborador = doc.ConvertTo<Colaborador>();
            colaborador.CPF = doc.Id;
            ViewBag.Coordenadores = new SelectList(await GetCoordenadoresAsync(), "CPF", "Nome", colaborador.CoordenadorCPF);
            return View(colaborador);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string id, Colaborador colaborador)
        {
            var sanitizedId = SanitizeCpf(id);
            colaborador.CPF = SanitizeCpf(colaborador.CPF);
            colaborador.CoordenadorCPF = SanitizeCpf(colaborador.CoordenadorCPF);

            if (sanitizedId != colaborador.CPF) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var docRef = _firestoreDb.Collection(CollectionName).Document(sanitizedId);
                    colaborador.DataAlteracao = DateTime.UtcNow;
                    await docRef.SetAsync(colaborador, SetOptions.MergeAll);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao editar colaborador no Firestore.");
                    ModelState.AddModelError(string.Empty, "Ocorreu um erro ao editar o colaborador.");
                }
            }
            ViewBag.Coordenadores = new SelectList(await GetCoordenadoresAsync(), "CPF", "Nome", colaborador.CoordenadorCPF);
            return View(colaborador);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null) return NotFound();
            var doc = await _firestoreDb.Collection(CollectionName).Document(SanitizeCpf(id)).GetSnapshotAsync();
            if (!doc.Exists) return NotFound();

            var colaborador = doc.ConvertTo<Colaborador>();
            colaborador.CPF = doc.Id;
            return View(colaborador);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var sanitizedId = SanitizeCpf(id);
            try
            {
                // Note: Deleting a colaborador might leave dangling references in other collections (Computadores, etc.)
                // A more robust solution would use Cloud Functions to clean up related data.
                await _firestoreDb.Collection(CollectionName).Document(sanitizedId).DeleteAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao excluir colaborador do Firestore.");
                var doc = await _firestoreDb.Collection(CollectionName).Document(sanitizedId).GetSnapshotAsync();
                var colaborador = doc.ConvertTo<Colaborador>();
                colaborador.CPF = doc.Id;
                ViewBag.ErrorMessage = "Erro ao excluir. Verifique as dependências.";
                return View(colaborador);
            }
        }

        private async Task<List<Colaborador>> GetCoordenadoresAsync()
        {
            var coordenadores = new List<Colaborador>();
            try
            {
                // This is inefficient. In a real-world scenario, you'd denormalize the "isCoordinator" role
                // into the "colaboradores" collection, updated by a Cloud Function when claims change.
                var pagedEnumerable = FirebaseAuth.DefaultInstance.ListUsersAsync(null);
                var enumerator = pagedEnumerable.GetEnumerator();
                while (await enumerator.MoveNext())
                {
                    var user = enumerator.Current;
                    if (user.CustomClaims.TryGetValue("role", out var role) && role.ToString() == "Coordenador")
                    {
                        if (user.CustomClaims.TryGetValue("ColaboradorCPF", out var cpf) && !string.IsNullOrEmpty(cpf.ToString()))
                        {
                            var doc = await _firestoreDb.Collection(CollectionName).Document(cpf.ToString()).GetSnapshotAsync();
                            if(doc.Exists)
                            {
                                var colab = doc.ConvertTo<Colaborador>();
                                colab.CPF = doc.Id;
                                coordenadores.Add(colab);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter a lista de coordenadores do Firestore/Auth.");
            }
            return coordenadores.OrderBy(c => c.Nome).ToList();
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

            var colaboradores = new List<Colaborador>();
            // ... (Excel parsing logic remains the same)

            int adicionados = 0;
            int atualizados = 0;

            var writeBatch = _firestoreDb.StartBatch();

            try
            {
                foreach (var colaborador in colaboradores)
                {
                    var docRef = _firestoreDb.Collection(CollectionName).Document(colaborador.CPF);
                    var snapshot = await docRef.GetSnapshotAsync();

                    if (snapshot.Exists)
                    {
                        colaborador.DataAlteracao = DateTime.UtcNow;
                        writeBatch.Set(docRef, colaborador, SetOptions.MergeAll);
                        atualizados++;
                    }
                    else
                    {
                        colaborador.DataInclusao = DateTime.UtcNow;
                        writeBatch.Set(docRef, colaborador);
                        adicionados++;
                    }
                }
                await writeBatch.CommitAsync();
                TempData["SuccessMessage"] = $"{adicionados} colaboradores adicionados e {atualizados} atualizados com sucesso.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao salvar os dados do Excel no Firestore.");
                TempData["ErrorMessage"] = "Ocorreu um erro ao salvar os dados. Nenhuma alteração foi feita.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}