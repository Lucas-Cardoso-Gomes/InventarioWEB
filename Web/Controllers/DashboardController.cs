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
            var viewModel = new DashboardViewModel();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Cards
                viewModel.ChamadosAbertos = await GetCountByStatusAsync(connection, "Aberto");
                viewModel.ChamadosEmAndamento = await GetCountByStatusAsync(connection, "Em Andamento");
                viewModel.ChamadosFechados = await GetCountByStatusAsync(connection, "Fechado");

                // Charts
                viewModel.Top10Servicos = await GetTop10ServicosAsync(connection);
                viewModel.PrioridadeServicos = await GetPrioridadeServicosAsync(connection);
                viewModel.Top10Usuarios = await GetTop10UsuariosAsync(connection);
                viewModel.HorarioMedioAbertura = await GetHorarioMedioAberturaAsync(connection);
            }

            return View(viewModel);
        }

        private async Task<int> GetCountByStatusAsync(SqlConnection connection, string status)
        {
            using (var cmd = new SqlCommand("SELECT COUNT(*) FROM Chamados WHERE Status = @Status", connection))
            {
                cmd.Parameters.AddWithValue("@Status", status);
                return (int)await cmd.ExecuteScalarAsync();
            }
        }

        private async Task<List<ChartData>> GetTop10ServicosAsync(SqlConnection connection)
        {
            var data = new List<ChartData>();
            using (var cmd = new SqlCommand("SELECT TOP 10 Servico, COUNT(*) as Count FROM Chamados GROUP BY Servico ORDER BY Count DESC", connection))
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    data.Add(new ChartData { Label = reader["Servico"].ToString(), Value = (int)reader["Count"] });
                }
            }
            return data;
        }

        private async Task<List<ChartData>> GetPrioridadeServicosAsync(SqlConnection connection)
        {
            var data = new List<ChartData>();
            using (var cmd = new SqlCommand("SELECT Prioridade, COUNT(*) as Count FROM Chamados GROUP BY Prioridade", connection))
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    data.Add(new ChartData { Label = reader["Prioridade"].ToString(), Value = (int)reader["Count"] });
                }
            }
            return data;
        }

        private async Task<List<ChartData>> GetTop10UsuariosAsync(SqlConnection connection)
        {
            var data = new List<ChartData>();
            string sql = @"SELECT TOP 10 co.Nome, COUNT(c.ID) as Count
                           FROM Chamados c
                           JOIN Colaboradores co ON c.ColaboradorCPF = co.CPF
                           GROUP BY co.Nome
                           ORDER BY Count DESC";
            using (var cmd = new SqlCommand(sql, connection))
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    data.Add(new ChartData { Label = reader["Nome"].ToString(), Value = (int)reader["Count"] });
                }
            }
            return data;
        }

        private async Task<List<ChartData>> GetHorarioMedioAberturaAsync(SqlConnection connection)
        {
            var data = new List<ChartData>();
            string sql = @"SELECT CAST(DATEPART(hour, DataCriacao) AS NVARCHAR(2)) + ':00' as Hour, COUNT(*) as Count
                           FROM Chamados
                           GROUP BY DATEPART(hour, DataCriacao)
                           ORDER BY DATEPART(hour, DataCriacao)";
            using (var cmd = new SqlCommand(sql, connection))
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    data.Add(new ChartData { Label = reader["Hour"].ToString(), Value = (int)reader["Count"] });
                }
            }
            return data;
        }
    }
}
