using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Web.Models;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using Google.Cloud.Firestore;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class RedesController : Controller
    {
        private readonly FirestoreDb _firestoreDb;
        private readonly ILogger<RedesController> _logger;
        private const string CollectionName = "redes";

        public RedesController(FirestoreDb firestoreDb, ILogger<RedesController> logger)
        {
            _firestoreDb = firestoreDb;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var redes = new List<Rede>();
            try
            {
                var snapshot = await _firestoreDb.Collection(CollectionName).GetSnapshotAsync();
                foreach (var doc in snapshot.Documents)
                {
                    var rede = doc.ConvertTo<Rede>();
                    rede.Id = doc.Id;
                    redes.Add(rede);
                }

                // Sort the list in-memory using System.Version for correct IP sorting
                var redesOrdenadas = redes.OrderBy(r => Version.Parse(r.IP)).ToList();
                return View(redesOrdenadas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting network assets list from Firestore.");
                return View(redes); // Return unsorted list in case of an error
            }
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Rede rede)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    rede.DataInclusao = DateTime.UtcNow;
                    await _firestoreDb.Collection(CollectionName).AddAsync(rede);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating network asset in Firestore.");
                    ModelState.AddModelError(string.Empty, "Ocorreu um erro ao salvar os dados.");
                }
            }
            return View(rede);
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var doc = await _firestoreDb.Collection(CollectionName).Document(id).GetSnapshotAsync();
            if (!doc.Exists) return NotFound();

            var rede = doc.ConvertTo<Rede>();
            rede.Id = doc.Id;
            return View(rede);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, Rede rede)
        {
            if (id != rede.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    rede.DataAlteracao = DateTime.UtcNow;
                    await _firestoreDb.Collection(CollectionName).Document(id).SetAsync(rede, SetOptions.MergeAll);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error updating network asset {id} in Firestore.");
                    ModelState.AddModelError(string.Empty, "Ocorreu um erro ao salvar os dados.");
                }
            }
            return View(rede);
        }

        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var doc = await _firestoreDb.Collection(CollectionName).Document(id).GetSnapshotAsync();
            if (!doc.Exists) return NotFound();

            var rede = doc.ConvertTo<Rede>();
            rede.Id = doc.Id;
            return View(rede);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            try
            {
                await _firestoreDb.Collection(CollectionName).Document(id).DeleteAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting network asset {id} from Firestore.");
                return RedirectToAction(nameof(Index));
            }
        }
    }
}