using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Web.Models;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Web.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly FirestoreDb _firestoreDb;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(FirestoreDb firestoreDb, ILogger<DashboardController> logger)
        {
            _firestoreDb = firestoreDb;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new DashboardViewModel
            {
                TotalComputadores = await GetTotalComputadoresCountAsync(),
                OpenChamados = await GetOpenChamadosCountAsync(),
                RecentManutencoes = await GetRecentManutencoesAsync(5)
            };

            return View(viewModel);
        }

        private async Task<int> GetTotalComputadoresCountAsync()
        {
            try
            {
                // Note: For very large collections, fetching all documents to count them is inefficient.
                // A better approach for large-scale apps is to maintain a counter in a separate document
                // that is updated via Cloud Functions. For this project's scale, this is acceptable.
                var snapshot = await _firestoreDb.Collection("computadores").GetSnapshotAsync();
                return snapshot.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total computer count from Firestore.");
                return 0;
            }
        }

        private async Task<int> GetOpenChamadosCountAsync()
        {
            try
            {
                var query = _firestoreDb.Collection("chamados").WhereIn("Status", new[] { "Aberto", "Em Andamento" });
                var snapshot = await query.GetSnapshotAsync();
                return snapshot.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting open chamados count from Firestore.");
                return 0;
            }
        }

        private async Task<IEnumerable<Manutencao>> GetRecentManutencoesAsync(int count)
        {
            var manutencoes = new List<Manutencao>();
            try
            {
                var query = _firestoreDb.Collection("manutencoes").OrderByDescending("Data").Limit(count);
                var snapshot = await query.GetSnapshotAsync();

                foreach (var doc in snapshot.Documents)
                {
                    var manutencao = doc.ConvertTo<Manutencao>();
                    manutencao.Id = doc.Id;

                    if (!string.IsNullOrEmpty(manutencao.ComputadorMAC))
                    {
                        var computadorDoc = await _firestoreDb.Collection("computadores").Document(manutencao.ComputadorMAC).GetSnapshotAsync();
                        if (computadorDoc.Exists)
                        {
                            manutencao.Computador = new Computador { Hostname = computadorDoc.GetValue<string>("Hostname") };
                        }
                    }
                    manutencoes.Add(manutencao);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent manutencoes from Firestore.");
            }
            return manutencoes;
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}