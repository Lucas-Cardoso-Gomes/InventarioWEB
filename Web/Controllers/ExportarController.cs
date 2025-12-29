using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Web.Models;
using Microsoft.AspNetCore.Authorization;
using Web.Services;
using System.Data;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin,Coordenador")]
    public class ExportarController : Controller
    {
        private readonly IDatabaseService _databaseService;
        private readonly ILogger<ExportarController> _logger;
        private readonly PersistentLogService _persistentLogService;

        public ExportarController(IDatabaseService databaseService, ILogger<ExportarController> logger, PersistentLogService persistentLogService)
        {
            _databaseService = databaseService;
            _logger = logger;
            _persistentLogService = persistentLogService;
        }

        public IActionResult Index()
        {
            var viewModel = new ExportarViewModel();
            using (var connection = _databaseService.CreateConnection())
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
                viewModel.Colaboradores = GetColaboradores(connection);
                viewModel.Coordenadores = GetCoordenadores(connection);
            }
            return View(viewModel);
        }

        private List<Colaborador> GetCoordenadores(IDbConnection connection)
        {
            var coordenadores = new List<Colaborador>();
            string sql;

            if (User.IsInRole("Admin"))
            {
                sql = "SELECT c.CPF, c.Nome FROM Colaboradores c INNER JOIN Usuarios u ON c.CPF = u.ColaboradorCPF WHERE u.Role = 'Coordenador' OR u.IsCoordinator = 1 ORDER BY c.Nome";
            }
            else
            {
                // Note: Parameters are not directly supported in SQL string here, need to handle in command execution
                sql = "SELECT c.CPF, c.Nome FROM Colaboradores c WHERE c.CPF = @UserCPF";
            }

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = sql;
                if (!User.IsInRole("Admin"))
                {
                    var userCpf = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var p = cmd.CreateParameter(); p.ParameterName = "@UserCPF"; p.Value = userCpf; cmd.Parameters.Add(p);
                }

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        coordenadores.Add(new Colaborador
                        {
                            CPF = reader["CPF"].ToString(),
                            Nome = reader["Nome"].ToString()
                        });
                    }
                }
            }
            return coordenadores;
        }

        private List<Colaborador> GetColaboradores(IDbConnection connection)
        {
            var colaboradores = new List<Colaborador>();
            string sql = "SELECT CPF, Nome FROM Colaboradores ORDER BY Nome";
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = sql;
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

        private List<string> GetDistinctValues(IDbConnection connection, string tableName, string columnName)
        {
            var values = new List<string>();
            var sql = $"SELECT DISTINCT {columnName} FROM {tableName} WHERE {columnName} IS NOT NULL ORDER BY {columnName}";
            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
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
        public async Task<IActionResult> Export(ExportarViewModel viewModel)
        {
            var csvBuilder = new StringBuilder();
            string fileName = $"export_{DateTime.Now:yyyyMMddHHmmss}.csv";
            string exportDetails = "";

            using (var connection = _databaseService.CreateConnection())
            {
                connection.Open();

                if (viewModel.ExportMode == ExportMode.PorDispositivo)
                {
                    exportDetails = $"Exported by Device Type: {viewModel.DeviceType}";
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
                            // Need to select ColaboradorNome via JOIN? The original code selected ColaboradorNome but query was SELECT {computerHeader} FROM Computadores.
                            // Computadores table doesn't have ColaboradorNome column. It seems the original code was bugged or relied on ViewModel mapping that isn't fully shown.
                            // Assuming Computadores table has fields or we need to join.
                            // Let's check Schema... Computadores has ColaboradorCPF.
                            // The original code `reader[col]` implies columns exist in result set.
                            // I should fix the query to include the join if ColaboradorNome is requested.

                            // To be safe and minimal change, I will use LEFT JOIN to get ColaboradorNome if it is in the header list.
                            // Original header has "ColaboradorNome".
                            sql = $"SELECT c.*, col.Nome as ColaboradorNome FROM Computadores c LEFT JOIN Colaboradores col ON c.ColaboradorCPF = col.CPF";

                            if (whereClauses.Any())
                            {
                                sql += " WHERE " + string.Join(" AND ", whereClauses);
                            }

                            using (var cmd = connection.CreateCommand())
                            {
                                cmd.CommandText = sql;
                                foreach (var p in parameters)
                                {
                                    var param = cmd.CreateParameter(); param.ParameterName = p.Key; param.Value = p.Value; cmd.Parameters.Add(param);
                                }

                                using (var reader = cmd.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        var line = new List<string>();
                                        foreach (var col in computerHeader.Split(','))
                                        {
                                            try {
                                                line.Add(reader[col].ToString());
                                            } catch {
                                                line.Add(""); // Handle missing column gracefully
                                            }
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
                            sql = $"SELECT m.*, col.Nome as ColaboradorNome FROM Monitores m LEFT JOIN Colaboradores col ON m.ColaboradorCPF = col.CPF";
                            if (whereClauses.Any())
                            {
                                sql += " WHERE " + string.Join(" AND ", whereClauses);
                            }

                            using (var cmd = connection.CreateCommand())
                            {
                                cmd.CommandText = sql;
                                foreach (var p in parameters)
                                {
                                    var param = cmd.CreateParameter(); param.ParameterName = p.Key; param.Value = p.Value; cmd.Parameters.Add(param);
                                }

                                using (var reader = cmd.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        var line = new List<string>();
                                        foreach (var col in monitorHeader.Split(','))
                                        {
                                            try {
                                                line.Add(reader[col].ToString());
                                            } catch {
                                                line.Add("");
                                            }
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
                            sql = $"SELECT p.*, col.Nome as ColaboradorNome FROM Perifericos p LEFT JOIN Colaboradores col ON p.ColaboradorCPF = col.CPF";
                            if (whereClauses.Any())
                            {
                                sql += " WHERE " + string.Join(" AND ", whereClauses);
                            }

                            using (var cmd = connection.CreateCommand())
                            {
                                cmd.CommandText = sql;
                                foreach (var p in parameters)
                                {
                                    var param = cmd.CreateParameter(); param.ParameterName = p.Key; param.Value = p.Value; cmd.Parameters.Add(param);
                                }

                                using (var reader = cmd.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        var line = new List<string>();
                                        foreach (var col in perifericoHeader.Split(','))
                                        {
                                            try {
                                                line.Add(reader[col].ToString());
                                            } catch {
                                                line.Add("");
                                            }
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
                    exportDetails = $"Exported by Collaborator CPF: {viewModel.SelectedColaboradorCPF}";
                    fileName = $"export_colaborador_{viewModel.SelectedColaboradorCPF}_{DateTime.Now:yyyyMMddHHmmss}.csv";

                    // Computadores
                    csvBuilder.AppendLine("Computadores");
                    csvBuilder.AppendLine("MAC,IP,Hostname,Fabricante,Processador,SO,DataColeta");
                    string sqlComputadores = "SELECT c.MAC, c.IP, c.Hostname, c.Fabricante, c.Processador, c.SO, c.DataColeta FROM Computadores c WHERE c.ColaboradorCPF = @colaboradorCpf";
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = sqlComputadores;
                        var p = cmd.CreateParameter(); p.ParameterName = "@colaboradorCpf"; p.Value = viewModel.SelectedColaboradorCPF; cmd.Parameters.Add(p);
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
                    string sqlMonitores = "SELECT m.PartNumber, col.Nome AS ColaboradorNome, m.Marca, m.Modelo, m.Tamanho FROM Monitores m INNER JOIN Colaboradores col ON m.ColaboradorCPF = col.CPF WHERE m.ColaboradorCPF = @colaboradorCpf";
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = sqlMonitores;
                        var p = cmd.CreateParameter(); p.ParameterName = "@colaboradorCpf"; p.Value = viewModel.SelectedColaboradorCPF; cmd.Parameters.Add(p);
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
                    string sqlPerifericos = "SELECT p.PartNumber, col.Nome AS ColaboradorNome, p.Tipo, p.DataEntrega FROM Perifericos p INNER JOIN Colaboradores col ON p.ColaboradorCPF = col.CPF WHERE p.ColaboradorCPF = @colaboradorCpf";
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = sqlPerifericos;
                        var p = cmd.CreateParameter(); p.ParameterName = "@colaboradorCpf"; p.Value = viewModel.SelectedColaboradorCPF; cmd.Parameters.Add(p);
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
                        LEFT JOIN Computadores c ON ma.ComputadorMAC = c.MAC AND c.ColaboradorCPF = @colaboradorCpf
                        LEFT JOIN Monitores m ON ma.MonitorPartNumber = m.PartNumber AND m.ColaboradorCPF = @colaboradorCpf
                        LEFT JOIN Perifericos p ON ma.PerifericoPartNumber = p.PartNumber AND p.ColaboradorCPF = @colaboradorCpf
                        WHERE c.ColaboradorCPF = @colaboradorCpf OR m.ColaboradorCPF = @colaboradorCpf OR p.ColaboradorCPF = @colaboradorCpf";

                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = sqlManutencoes;
                        var p = cmd.CreateParameter(); p.ParameterName = "@colaboradorCpf"; p.Value = viewModel.SelectedColaboradorCPF; cmd.Parameters.Add(p);
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
                else if (viewModel.ExportMode == ExportMode.PorCoordenador)
                {
                    exportDetails = $"Exported by Coordinator CPF: {viewModel.CoordenadorCPF}";
                    fileName = $"export_coordenador_{viewModel.CoordenadorCPF}_{DateTime.Now:yyyyMMddHHmmss}.csv";
                    string computerHeader = "MAC,IP,ColaboradorNome,Hostname,Fabricante,Processador,ProcessadorFabricante,ProcessadorCore,ProcessadorThread,ProcessadorClock,Ram,RamTipo,RamVelocidade,RamVoltagem,RamPorModule,ArmazenamentoC,ArmazenamentoCTotal,ArmazenamentoCLivre,ArmazenamentoD,ArmazenamentoDTotal,ArmazenamentoDLivre,ConsumoCPU,SO,DataColeta";
                    csvBuilder.AppendLine(computerHeader);

                    string sql = $@"
                        SELECT c.MAC, c.IP, colab.Nome as ColaboradorNome, c.Hostname, c.Fabricante, c.Processador, c.ProcessadorFabricante, c.ProcessadorCore, c.ProcessadorThread, c.ProcessadorClock, c.Ram, c.RamTipo, c.RamVelocidade, c.RamVoltagem, c.RamPorModule, c.ArmazenamentoC, c.ArmazenamentoCTotal, c.ArmazenamentoCLivre, c.ArmazenamentoD, c.ArmazenamentoDTotal, c.ArmazenamentoDLivre, c.ConsumoCPU, c.SO, c.DataColeta
                        FROM Computadores c
                        INNER JOIN Colaboradores colab ON c.ColaboradorCPF = colab.CPF
                        WHERE colab.CoordenadorCPF = @coordenadorCpf OR colab.CPF = @coordenadorCpf";

                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = sql;
                        var p = cmd.CreateParameter(); p.ParameterName = "@coordenadorCpf"; p.Value = viewModel.CoordenadorCPF; cmd.Parameters.Add(p);

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
                }
            }

            await _persistentLogService.LogChangeAsync(
                User.Identity.Name,
                "EXPORT",
                "Data",
                "Exported data to CSV",
                exportDetails
            );

            return File(Encoding.UTF8.GetBytes(csvBuilder.ToString()), "text/csv", fileName);
        }
    }
}
