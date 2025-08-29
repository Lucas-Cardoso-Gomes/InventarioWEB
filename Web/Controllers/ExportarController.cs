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
    [Authorize(Roles = "Admin,Coordenador")]
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
            var viewModel = new ExportarViewModel();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                // Computer filters
                viewModel.Fabricantes = GetDistinctValues(connection, "Computadores", "Fabricante");
                viewModel.SOs = GetDistinctValues(connection, "Computadores", "SO");
                viewModel.ProcessadorFabricantes = GetDistinctValues(connection, "Computadores", "ProcessadorFabricante");
                viewModel.RamTipos = GetDistinctValues(connection, "Computadores", "RamTipo");
                viewModel.Processadores = GetDistinctValues(connection, "Computadores", "Processador");
                viewModel.Rams = GetDistinctValues(connection, "Computadores", "Ram");

                // Monitor filters
                viewModel.Marcas = GetDistinctValues(connection, "Monitores", "Marca");
                viewModel.Tamanhos = GetDistinctValues(connection, "Monitores", "Tamanho");
                viewModel.Modelos = GetDistinctValues(connection, "Monitores", "Modelo");

                // Periferico filters
                viewModel.TiposPeriferico = GetDistinctValues(connection, "Perifericos", "Tipo");

                // Colaborador filter
                viewModel.Colaboradores = GetColaboradores(connection).Select(c => c.Nome).ToList();
            }
            return View(viewModel);
        }

        private List<Colaborador> GetColaboradores(SqlConnection connection)
        {
            var colaboradores = new List<Colaborador>();
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
            return colaboradores;
        }

        private List<string> GetDistinctValues(SqlConnection connection, string tableName, string columnName)
        {
            var values = new List<string>();
            var sql = $"SELECT DISTINCT {columnName} FROM {tableName} WHERE {columnName} IS NOT NULL ORDER BY {columnName}";
            using (var command = new SqlCommand(sql, connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        values.Add(reader[0].ToString());
                    }
                }
            }
            return values;
        }

        [HttpPost]
        public IActionResult Export(ExportarViewModel viewModel)
        {
            var csvBuilder = new StringBuilder();
            string fileName = $"export_{DateTime.Now:yyyyMMddHHmmss}.csv";

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                if (viewModel.ExportMode == ExportMode.PorDispositivo)
                {
                    fileName = $"export_{viewModel.DeviceType}_{DateTime.Now:yyyyMMddHHmmss}.csv";
                    string sql = "";
                    var whereClauses = new List<string>();
                    var parameters = new Dictionary<string, object>();

                    Action<string, List<string>> addInClause = (columnName, values) =>
                    {
                        if (values != null && values.Any())
                        {
                            var paramNames = new List<string>();
                            for (int i = 0; i < values.Count; i++)
                            {
                                var paramName = $"@{columnName.ToLower().Replace(" ", "")}{i}";
                                paramNames.Add(paramName);
                                parameters.Add(paramName, values[i]);
                            }
                            whereClauses.Add($"{columnName} IN ({string.Join(", ", paramNames)})");
                        }
                    };

                    switch (viewModel.DeviceType)
                    {
                        case DeviceType.Computadores:
                            addInClause("Fabricante", viewModel.CurrentFabricantes);
                            addInClause("SO", viewModel.CurrentSOs);
                            addInClause("ProcessadorFabricante", viewModel.CurrentProcessadorFabricantes);
                            addInClause("RamTipo", viewModel.CurrentRamTipos);
                            addInClause("Processador", viewModel.CurrentProcessadores);
                            addInClause("Ram", viewModel.CurrentRams);

                            string computerHeader = "MAC,IP,ColaboradorNome,Hostname,Fabricante,Processador,ProcessadorFabricante,ProcessadorCore,ProcessadorThread,ProcessadorClock,Ram,RamTipo,RamVelocidade,RamVoltagem,RamPorModule,ArmazenamentoC,ArmazenamentoCTotal,ArmazenamentoCLivre,ArmazenamentoD,ArmazenamentoDTotal,ArmazenamentoDLivre,ConsumoCPU,SO,DataColeta";
                            csvBuilder.AppendLine(computerHeader);
                            sql = $"SELECT {computerHeader} FROM Computadores";
                            if (whereClauses.Any())
                            {
                                sql += " WHERE " + string.Join(" AND ", whereClauses);
                            }

                            using (var cmd = new SqlCommand(sql, connection))
                            {
                                foreach (var p in parameters)
                                {
                                    cmd.Parameters.AddWithValue(p.Key, p.Value);
                                }

                                using (var reader = cmd.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        var line = new List<string>();
                                        foreach (var col in computerHeader.Split(','))
                                        {
                                            line.Add(reader[col].ToString());
                                        }
                                        csvBuilder.AppendLine(string.Join(",", line));
                                    }
                                }
                            }
                            break;

                        case DeviceType.Monitores:
                            addInClause("Marca", viewModel.CurrentMarcas);
                            addInClause("Tamanho", viewModel.CurrentTamanhos);
                            addInClause("Modelo", viewModel.CurrentModelos);

                            string monitorHeader = "PartNumber,ColaboradorNome,Marca,Modelo,Tamanho";
                            csvBuilder.AppendLine(monitorHeader);
                            sql = $"SELECT {monitorHeader} FROM Monitores";
                            if (whereClauses.Any())
                            {
                                sql += " WHERE " + string.Join(" AND ", whereClauses);
                            }

                            using (var cmd = new SqlCommand(sql, connection))
                            {
                                foreach (var p in parameters)
                                {
                                    cmd.Parameters.AddWithValue(p.Key, p.Value);
                                }

                                using (var reader = cmd.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        var line = new List<string>();
                                        foreach (var col in monitorHeader.Split(','))
                                        {
                                            line.Add(reader[col].ToString());
                                        }
                                        csvBuilder.AppendLine(string.Join(",", line));
                                    }
                                }
                            }
                            break;

                        case DeviceType.Perifericos:
                            addInClause("Tipo", viewModel.CurrentTiposPeriferico);

                            string perifericoHeader = "PartNumber,ColaboradorNome,Tipo,DataEntrega";
                            csvBuilder.AppendLine(perifericoHeader);
                            sql = $"SELECT {perifericoHeader} FROM Perifericos";
                            if (whereClauses.Any())
                            {
                                sql += " WHERE " + string.Join(" AND ", whereClauses);
                            }

                            using (var cmd = new SqlCommand(sql, connection))
                            {
                                foreach (var p in parameters)
                                {
                                    cmd.Parameters.AddWithValue(p.Key, p.Value);
                                }

                                using (var reader = cmd.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        var line = new List<string>();
                                        foreach (var col in perifericoHeader.Split(','))
                                        {
                                            line.Add(reader[col].ToString());
                                        }
                                        csvBuilder.AppendLine(string.Join(",", line));
                                    }
                                }
                            }
                            break;
                    }
                }
                else if (viewModel.ExportMode == ExportMode.PorColaborador)
                {
                    fileName = $"export_colaborador_{viewModel.ColaboradorNome}_{DateTime.Now:yyyyMMddHHmmss}.csv";

                    // Computadores
                    csvBuilder.AppendLine("Computadores");
                    csvBuilder.AppendLine("MAC,IP,Hostname,Fabricante,Processador,SO,DataColeta");
                    string sqlComputadores = "SELECT MAC, IP, Hostname, Fabricante, Processador, SO, DataColeta FROM Computadores WHERE ColaboradorNome = @colaborador";
                    using (var cmd = new SqlCommand(sqlComputadores, connection))
                    {
                        cmd.Parameters.AddWithValue("@colaborador", viewModel.ColaboradorNome);
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
                    csvBuilder.AppendLine("PartNumber,ColaboradorNome,Marca,Modelo,Tamanho");
                    string sqlMonitores = "SELECT PartNumber, ColaboradorNome, Marca, Modelo, Tamanho FROM Monitores WHERE ColaboradorNome = @colaborador";
                    using (var cmd = new SqlCommand(sqlMonitores, connection))
                    {
                        cmd.Parameters.AddWithValue("@colaborador", viewModel.ColaboradorNome);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                csvBuilder.AppendLine($"{reader["PartNumber"]},{reader["ColaboradorNome"]},{reader["Marca"]},{reader["Modelo"]},{reader["Tamanho"]}");
                            }
                        }
                    }

                    // Perifericos
                    csvBuilder.AppendLine();
                    csvBuilder.AppendLine("Perifericos");
                    csvBuilder.AppendLine("PartNumber,ColaboradorNome,Tipo,DataEntrega");
                    string sqlPerifericos = "SELECT PartNumber, ColaboradorNome, Tipo, DataEntrega FROM Perifericos WHERE ColaboradorNome = @colaborador";
                    using (var cmd = new SqlCommand(sqlPerifericos, connection))
                    {
                        cmd.Parameters.AddWithValue("@colaborador", viewModel.ColaboradorNome);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                csvBuilder.AppendLine($"{reader["PartNumber"]},{reader["ColaboradorNome"]},{reader["Tipo"]},{reader["DataEntrega"]}");
                            }
                        }
                    }

                    // Manutenções
                    csvBuilder.AppendLine();
                    csvBuilder.AppendLine("Manutenções");
                    csvBuilder.AppendLine("Equipamento,Tipo,DataManutencaoHardware,DataManutencaoSoftware,ManutencaoExterna,Data,Historico");
                    string sqlManutencoes = @"
                        SELECT
                            COALESCE(c.MAC, m.PartNumber, p.PartNumber) as Equipamento,
                            CASE
                                WHEN c.MAC IS NOT NULL THEN 'Computador'
                                WHEN m.PartNumber IS NOT NULL THEN 'Monitor'
                                WHEN p.PartNumber IS NOT NULL THEN 'Periferico'
                            END as Tipo,
                            ma.DataManutencaoHardware,
                            ma.DataManutencaoSoftware,
                            ma.ManutencaoExterna,
                            ma.Data,
                            ma.Historico
                        FROM Manutencoes ma
                        LEFT JOIN Computadores c ON ma.ComputadorMAC = c.MAC AND c.ColaboradorNome = @colaborador
                        LEFT JOIN Monitores m ON ma.MonitorPartNumber = m.PartNumber AND m.ColaboradorNome = @colaborador
                        LEFT JOIN Perifericos p ON ma.PerifericoPartNumber = p.PartNumber AND p.ColaboradorNome = @colaborador
                        WHERE c.ColaboradorNome = @colaborador OR m.ColaboradorNome = @colaborador OR p.ColaboradorNome = @colaborador";

                    using (var cmd = new SqlCommand(sqlManutencoes, connection))
                    {
                        cmd.Parameters.AddWithValue("@colaborador", viewModel.ColaboradorNome);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var line = new List<string>
                                {
                                    reader["Equipamento"].ToString(),
                                    reader["Tipo"].ToString(),
                                    reader["DataManutencaoHardware"].ToString(),
                                    reader["DataManutencaoSoftware"].ToString(),
                                    reader["ManutencaoExterna"].ToString(),
                                    reader["Data"].ToString(),
                                    reader["Historico"].ToString()
                                };
                                csvBuilder.AppendLine(string.Join(",", line));
                            }
                        }
                    }
                }
            }

            return File(Encoding.UTF8.GetBytes(csvBuilder.ToString()), "text/csv", fileName);
        }
    }
}
