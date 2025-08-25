using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using web.Models;
using Microsoft.AspNetCore.Authorization;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin,Coordenador,Normal")]
    public class ExportarController : Controller
    {
        private readonly string _connectionString;
        private readonly ILogger<ExportarController> _logger;

        public ExportarController(IConfiguration configuration, ILogger<ExportarController> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
        }

        public IActionResult Index()
        {
            var viewModel = new ExportarViewModel
            {
                Colaboradores = GetColaboradores().Select(c => c.Nome).ToList()
            };
            return View(viewModel);
        }

        private List<Colaborador> GetColaboradores()
        {
            var colaboradores = new List<Colaborador>();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string sql = "SELECT CPF, Nome FROM Colaboradores ORDER BY Nome";
                using (var cmd = new SqlCommand(sql, connection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            colaboradores.Add(new Colaborador
                            {
                                CPF = reader["CPF"].ToString(),
                                Nome = reader["Nome"].ToString()
                            });
                        }
                    }
                }
            }
            return colaboradores;
        }

        [HttpPost]
        public IActionResult Export(ExportarViewModel viewModel)
        {
            var csvBuilder = new StringBuilder();
            string fileName = $"export_{DateTime.Now:yyyyMMddHHmmss}.csv";

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                switch (viewModel.ExportType)
                {
                    case ExportType.EquipamentosPorColaborador:
                        fileName = $"equipamentos_{viewModel.FilterValue}_{DateTime.Now:yyyyMMddHHmmss}.csv";
                        // Computadores
                        csvBuilder.AppendLine("Computadores");
                        csvBuilder.AppendLine("MAC,IP,Hostname,Fabricante,Processador,SO,DataColeta");
                        string sqlComputadores = "SELECT MAC, IP, Hostname, Fabricante, Processador, SO, DataColeta FROM Computadores WHERE ColaboradorNome = @colaborador";
                        using (var cmd = new SqlCommand(sqlComputadores, connection))
                        {
                            cmd.Parameters.AddWithValue("@colaborador", viewModel.FilterValue);
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    csvBuilder.AppendLine($"{reader["MAC"]},{reader["IP"]},{reader["Hostname"]},{reader["Fabricante"]},{reader["Processador"]},{reader["SO"]},{reader["DataColeta"]}");
                                }
                            }
                        }

                        // Monitores
                        csvBuilder.AppendLine();
                        csvBuilder.AppendLine("Monitores");
                        csvBuilder.AppendLine("PartNumber,Marca,Modelo,Tamanho");
                        string sqlMonitores = "SELECT PartNumber, Marca, Modelo, Tamanho FROM Monitores WHERE ColaboradorNome = @colaborador";
                        using (var cmd = new SqlCommand(sqlMonitores, connection))
                        {
                            cmd.Parameters.AddWithValue("@colaborador", viewModel.FilterValue);
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    csvBuilder.AppendLine($"{reader["PartNumber"]},{reader["Marca"]},{reader["Modelo"]},{reader["Tamanho"]}");
                                }
                            }
                        }

                        // Perifericos
                        csvBuilder.AppendLine();
                        csvBuilder.AppendLine("Perifericos");
                        csvBuilder.AppendLine("Tipo,PartNumber,DataEntrega");
                        string sqlPerifericos = "SELECT Tipo, PartNumber, DataEntrega FROM Perifericos WHERE ColaboradorNome = @colaborador";
                        using (var cmd = new SqlCommand(sqlPerifericos, connection))
                        {
                            cmd.Parameters.AddWithValue("@colaborador", viewModel.FilterValue);
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    csvBuilder.AppendLine($"{reader["Tipo"]},{reader["PartNumber"]},{reader["DataEntrega"]}");
                                }
                            }
                        }
                        break;

                    case ExportType.ComputadoresPorProcessador:
                        fileName = $"computadores_processador_{viewModel.FilterValue}_{DateTime.Now:yyyyMMddHHmmss}.csv";
                        csvBuilder.AppendLine("MAC,IP,Hostname,Fabricante,Processador,SO,DataColeta");
                        string sqlProcessador = "SELECT MAC, IP, Hostname, Fabricante, Processador, SO, DataColeta FROM Computadores WHERE Processador LIKE @processador";
                        using (var cmd = new SqlCommand(sqlProcessador, connection))
                        {
                            cmd.Parameters.AddWithValue("@processador", $"%{viewModel.FilterValue}%");
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    csvBuilder.AppendLine($"{reader["MAC"]},{reader["IP"]},{reader["Hostname"]},{reader["Fabricante"]},{reader["Processador"]},{reader["SO"]},{reader["DataColeta"]}");
                                }
                            }
                        }
                        break;

                    case ExportType.ComputadoresPorTamanhoMonitor:
                        fileName = $"computadores_monitor_{viewModel.FilterValue}_{DateTime.Now:yyyyMMddHHmmss}.csv";
                        csvBuilder.AppendLine("MAC,IP,Hostname,Fabricante,Processador,SO,DataColeta,TamanhoMonitor");
                        string sqlMonitor = @"
                            SELECT c.MAC, c.IP, c.Hostname, c.Fabricante, c.Processador, c.SO, c.DataColeta, m.Tamanho
                            FROM Computadores c
                            JOIN Monitores m ON c.ColaboradorNome = m.ColaboradorNome
                            WHERE m.Tamanho = @tamanho";
                        using (var cmd = new SqlCommand(sqlMonitor, connection))
                        {
                            cmd.Parameters.AddWithValue("@tamanho", viewModel.FilterValue);
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    csvBuilder.AppendLine($"{reader["MAC"]},{reader["IP"]},{reader["Hostname"]},{reader["Fabricante"]},{reader["Processador"]},{reader["SO"]},{reader["DataColeta"]},{reader["Tamanho"]}");
                                }
                            }
                        }
                        break;
                }
            }

            return File(Encoding.UTF8.GetBytes(csvBuilder.ToString()), "text/csv", fileName);
        }
    }
}
