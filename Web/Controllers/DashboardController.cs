using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Web.Models;
using Web.Services;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;

namespace Web.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ColetaService _coletaService;
        private readonly ManutencaoService _manutencaoService;
        private readonly string _connectionString;

        public DashboardController(ColetaService coletaService, ManutencaoService manutencaoService, IConfiguration configuration)
        {
            _coletaService = coletaService;
            _manutencaoService = manutencaoService;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<IActionResult> Index()
        {
            var totalComputadores = await _coletaService.GetTotalComputadoresAsync();
            var recentManutencoes = await _manutencaoService.GetRecentManutencoesAsync(5);
            var openChamados = await GetOpenChamadosCountAsync();

            var viewModel = new DashboardViewModel
            {
                TotalComputadores = totalComputadores,
                RecentManutencoes = recentManutencoes,
                OpenChamados = openChamados
            };

            return View(viewModel);
        }

        private async Task<int> GetOpenChamadosCountAsync()
        {
            int count = 0;
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    string sql = "SELECT COUNT(*) FROM Chamados WHERE AdminCPF IS NULL";
                    using (var cmd = new SqlCommand(sql, connection))
                    {
                        count = (int)await cmd.ExecuteScalarAsync();
                    }
                }
            }
            catch (Exception)
            {
                // Log the exception in a real application
                // For now, just return 0
                count = 0;
            }
            return count;
        }
    }
}
