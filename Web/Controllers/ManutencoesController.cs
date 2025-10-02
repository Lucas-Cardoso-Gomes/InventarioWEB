using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Web.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin,Coordenador")]
    public class ManutencoesController : Controller
    {
        private readonly FirestoreDb _firestoreDb;
        private readonly ILogger<ManutencoesController> _logger;
        private const string CollectionName = "manutencoes";

        public ManutencoesController(FirestoreDb firestoreDb, ILogger<ManutencoesController> logger)
        {
            _firestoreDb = firestoreDb;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string partNumber, string colaborador, string hostname)
        {
            var manutencoes = new List<Manutencao>();
            try
            {
                Query query = _firestoreDb.Collection(CollectionName);
                var snapshot = await query.GetSnapshotAsync();

                foreach(var doc in snapshot.Documents)
                {
                    var manutencao = doc.ConvertTo<Manutencao>();
                    manutencao.Id = doc.Id;

                    // Enrich data for display/filtering
                    if(!string.IsNullOrEmpty(manutencao.ComputadorMAC))
                    {
                        var compDoc = await _firestoreDb.Collection("computadores").Document(manutencao.ComputadorMAC).GetSnapshotAsync();
                        if(compDoc.Exists) manutencao.Computador = compDoc.ConvertTo<Computador>();
                    }
                    if(!string.IsNullOrEmpty(manutencao.MonitorPartNumber))
                    {
                        var monitorDoc = await _firestoreDb.Collection("monitores").Document(manutencao.MonitorPartNumber).GetSnapshotAsync();
                        if(monitorDoc.Exists) manutencao.Monitor = monitorDoc.ConvertTo<Monitor>();
                    }
                    if(!string.IsNullOrEmpty(manutencao.PerifericoPartNumber))
                    {
                        var perDoc = await _firestoreDb.Collection("perifericos").Document(manutencao.PerifericoPartNumber).GetSnapshotAsync();
                        if(perDoc.Exists) manutencao.Periferico = perDoc.ConvertTo<Periferico>();
                    }

                    manutencoes.Add(manutencao);
                }

                // Apply filters in-memory
                if (!string.IsNullOrEmpty(partNumber))
                    manutencoes = manutencoes.Where(m => m.ComputadorMAC == partNumber || m.MonitorPartNumber == partNumber || m.PerifericoPartNumber == partNumber).ToList();
                if (!string.IsNullOrEmpty(hostname))
                    manutencoes = manutencoes.Where(m => m.Computador?.Hostname.Contains(hostname, StringComparison.OrdinalIgnoreCase) == true).ToList();

            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error getting maintenance list from Firestore.");
            }

            var viewModel = new ManutencaoIndexViewModel
            {
                Manutencoes = manutencoes.OrderByDescending(m => m.Data),
                PartNumber = partNumber,
                Colaborador = colaborador, // Note: Colaborador filter not implemented due to complexity
                Hostname = hostname
            };

            return View(viewModel);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            await PopulateViewData();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Manutencao manutencao)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    manutencao.Data = DateTime.UtcNow;
                    await _firestoreDb.Collection(CollectionName).AddAsync(manutencao);
                    return RedirectToAction(nameof(Index));
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex, "Error creating maintenance record in Firestore.");
                    ModelState.AddModelError("", "An error occurred while creating the record.");
                }
            }
            await PopulateViewData(manutencao);
            return View(manutencao);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            var doc = await _firestoreDb.Collection(CollectionName).Document(id).GetSnapshotAsync();
            if (!doc.Exists) return NotFound();

            var manutencao = doc.ConvertTo<Manutencao>();
            manutencao.Id = doc.Id;

            await PopulateViewData(manutencao);
            return View(manutencao);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string id, Manutencao manutencao)
        {
            if (id != manutencao.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    manutencao.DataAlteracao = DateTime.UtcNow;
                    await _firestoreDb.Collection(CollectionName).Document(id).SetAsync(manutencao, SetOptions.MergeAll);
                    return RedirectToAction(nameof(Index));
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex, $"Error updating maintenance record {id} in Firestore.");
                    ModelState.AddModelError("", "An error occurred while updating the record.");
                }
            }
            await PopulateViewData(manutencao);
            return View(manutencao);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            var doc = await _firestoreDb.Collection(CollectionName).Document(id).GetSnapshotAsync();
            if (!doc.Exists) return NotFound();

            var manutencao = doc.ConvertTo<Manutencao>();
            manutencao.Id = doc.Id;
            return View(manutencao);
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
                _logger.LogError(ex, $"Error deleting maintenance record {id} from Firestore.");
                // Redirect with error message
                return RedirectToAction(nameof(Index));
            }
        }

        private async Task PopulateViewData(Manutencao manutencao = null)
        {
            var computadores = await GetComputadoresAsync();
            var monitores = await GetMonitoresAsync();
            var perifericos = await GetPerifericosAsync();

            ViewData["ComputadorMAC"] = new SelectList(computadores.Select(c => new { Value = c.MAC, Text = $"{c.Hostname} ({c.MAC})" }), "Value", "Text", manutencao?.ComputadorMAC);
            ViewData["MonitorPartNumber"] = new SelectList(monitores.Select(m => new { Value = m.PartNumber, Text = $"{m.Modelo} ({m.PartNumber})" }), "Value", "Text", manutencao?.MonitorPartNumber);
            ViewData["PerifericoPartNumber"] = new SelectList(perifericos.Select(p => new { Value = p.PartNumber, Text = $"{p.Tipo} ({p.PartNumber})" }), "Value", "Text", manutencao?.PerifericoPartNumber);
        }

        private async Task<List<Computador>> GetComputadoresAsync()
        {
            var computadores = new List<Computador>();
            var snapshot = await _firestoreDb.Collection("computadores").OrderBy("Hostname").GetSnapshotAsync();
            foreach(var doc in snapshot.Documents)
            {
                computadores.Add(new Computador { MAC = doc.Id, Hostname = doc.GetValue<string>("Hostname") });
            }
            return computadores;
        }

        private async Task<List<Monitor>> GetMonitoresAsync()
        {
            var monitores = new List<Monitor>();
            var snapshot = await _firestoreDb.Collection("monitores").OrderBy("Modelo").GetSnapshotAsync();
            foreach(var doc in snapshot.Documents)
            {
                monitores.Add(new Monitor { PartNumber = doc.Id, Modelo = doc.GetValue<string>("Modelo") });
            }
            return monitores;
        }

        private async Task<List<Periferico>> GetPerifericosAsync()
        {
            var perifericos = new List<Periferico>();
            var snapshot = await _firestoreDb.Collection("perifericos").OrderBy("Tipo").GetSnapshotAsync();
            foreach(var doc in snapshot.Documents)
            {
                perifericos.Add(new Periferico { PartNumber = doc.Id, Tipo = doc.GetValue<string>("Tipo") });
            }
            return perifericos;
        }
    }
}