using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Web.Models;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin,Coordenador,Diretoria")]
    public class DashboardController : Controller
    {
        private readonly string _connectionString;

        public DashboardController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
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
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Computadores", connection))
                    {
                        return (int)await cmd.ExecuteScalarAsync();
                    }
                }
            }
            catch (Exception)
            {
                // Log exception
                return 0;
            }
        }

        private async Task<int> GetOpenChamadosCountAsync()
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    string sql = "SELECT COUNT(*) FROM Chamados WHERE Status IN ('Aberto', 'Em Andamento')";
                    using (var cmd = new SqlCommand(sql, connection))
                    {
                        return (int)await cmd.ExecuteScalarAsync();
                    }
                }
            }
            catch (Exception)
            {
                // Log exception
                return 0;
            }
        }

        private async Task<IEnumerable<Manutencao>> GetRecentManutencoesAsync(int count)
        {
            var manutencoes = new List<Manutencao>();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    string sql = @"
                        SELECT TOP (@Count)
                            m.Id, m.Data, m.Historico, m.ComputadorMAC,
                            c.Hostname
                        FROM Manutencoes m
                        LEFT JOIN Computadores c ON m.ComputadorMAC = c.MAC
                        ORDER BY m.Data DESC";

                    using (var command = new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@Count", count);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var manutencao = new Manutencao
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                    Data = reader.IsDBNull(reader.GetOrdinal("Data")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("Data")),
                                    Historico = reader.IsDBNull(reader.GetOrdinal("Historico")) ? null : reader.GetString(reader.GetOrdinal("Historico")),
                                    ComputadorMAC = reader.IsDBNull(reader.GetOrdinal("ComputadorMAC")) ? null : reader.GetString(reader.GetOrdinal("ComputadorMAC"))
                                };

                                if (!reader.IsDBNull(reader.GetOrdinal("Hostname")))
                                {
                                    manutencao.Computador = new Computador { Hostname = reader.GetString(reader.GetOrdinal("Hostname")) };
                                }
                                manutencoes.Add(manutencao);
                            }
                        }
                    }
                }
            }
            catch(Exception)
            {
                // Log exception
            }
            return manutencoes;
        }
    }
}
