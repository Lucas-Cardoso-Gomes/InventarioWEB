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
using Web.Services;
using System.Threading.Tasks;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin,Coordenador")]
    public class ExportarController : Controller
    {
        private readonly string _connectionString;
        private readonly ILogger<ExportarController> _logger;
        private readonly UserService _userService;

        public ExportarController(IConfiguration configuration, ILogger<ExportarController> logger, UserService userService)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
            _userService = userService;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new ExportarViewModel();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var whereClauses = new List<string>();
                var parameters = new Dictionary<string, object>();

                var user = await _userService.FindByLoginAsync(User.Identity.Name);
                if (User.IsInRole("Coordenador"))
                {
                    var colaboradores = await _userService.GetColaboradoresByCoordenadorAsync(user.Id);
                    var cpfs = colaboradores.Select(c => c.ColaboradorCPF).ToList();
                    if (user.ColaboradorCPF != null)
                    {
                        cpfs.Add(user.ColaboradorCPF);
                    }
                    if (cpfs.Any())
                    {
                        var cpfParams = new List<string>();
                        for (int i = 0; i < cpfs.Count; i++)
                        {
                            var paramName = $"@cpf{i}";
                            cpfParams.Add(paramName);
                            parameters.Add(paramName, cpfs[i]);
                        }
                        whereClauses.Add($"ColaboradorCPF IN ({string.Join(", ", cpfParams)})");
                    }
                }

                string whereSql = whereClauses.Any() ? $"WHERE {string.Join(" AND ", whereClauses)}" : "";

                // Computer filters
                viewModel.Fabricantes = GetDistinctValues(connection, "Computadores", "Fabricante", whereSql, parameters);
                viewModel.SOs = GetDistinctValues(connection, "Computadores", "SO", whereSql, parameters);
                viewModel.ProcessadorFabricantes = GetDistinctValues(connection, "Computadores", "ProcessadorFabricante", whereSql, parameters);
                viewModel.RamTipos = GetDistinctValues(connection, "Computadores", "RamTipo", whereSql, parameters);
                viewModel.Processadores = GetDistinctValues(connection, "Computadores", "Processador", whereSql, parameters);
                viewModel.Rams = GetDistinctValues(connection, "Computadores", "Ram", whereSql, parameters);

                // Monitor filters
                viewModel.Marcas = GetDistinctValues(connection, "Monitores", "Marca", whereSql, parameters);
                viewModel.Tamanhos = GetDistinctValues(connection, "Monitores", "Tamanho", whereSql, parameters);
                viewModel.Modelos = GetDistinctValues(connection, "Monitores", "Modelo", whereSql, parameters);

                // Periferico filters
                viewModel.TiposPeriferico = GetDistinctValues(connection, "Perifericos", "Tipo", whereSql, parameters);

                // Colaborador filter
                viewModel.Colaboradores = GetColaboradores(connection, whereSql, parameters).Select(c => c.Nome).ToList();
            }
            return View(viewModel);
        }

        private List<Colaborador> GetColaboradores(SqlConnection connection, string whereSql, Dictionary<string, object> parameters)
        {
            var colaboradores = new List<Colaborador>();
            string sql = $"SELECT CPF, Nome FROM Colaboradores {whereSql} ORDER BY Nome";
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

        private List<string> GetDistinctValues(SqlConnection connection, string tableName, string columnName, string whereSql, Dictionary<string, object> parameters)
        {
            var values = new List<string>();
            var sql = $"SELECT DISTINCT {columnName} FROM {tableName} ";
            if (!string.IsNullOrEmpty(whereSql))
            {
                sql += whereSql + " AND ";
            }
            else
            {
                sql += " WHERE ";
            }
            sql += $"{columnName} IS NOT NULL ORDER BY {columnName}";

            using (var command = new SqlCommand(sql, connection))
            {
                foreach (var p in parameters)
                {
                    command.Parameters.AddWithValue(p.Key, p.Value);
                }
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

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var whereClauses = new List<string>();
                var parameters = new Dictionary<string, object>();

                var user = await _userService.FindByLoginAsync(User.Identity.Name);
                if (User.IsInRole("Coordenador"))
                {
                    var colaboradores = await _userService.GetColaboradoresByCoordenadorAsync(user.Id);
                    var cpfs = colaboradores.Select(c => c.ColaboradorCPF).ToList();
                    if (user.ColaboradorCPF != null)
                    {
                        cpfs.Add(user.ColaboradorCPF);
                    }
                    if (cpfs.Any())
                    {
                        var cpfParams = new List<string>();
                        for (int i = 0; i < cpfs.Count; i++)
                        {
                            var paramName = $"@cpf{i}";
                            cpfParams.Add(paramName);
                            parameters.Add(paramName, cpfs[i]);
                        }
                        whereClauses.Add($"ColaboradorCPF IN ({string.Join(", ", cpfParams)})");
                    }
                }

                if (viewModel.ExportMode == ExportMode.PorDispositivo)
                {
                    fileName = $"export_{viewModel.DeviceType}_{DateTime.Now:yyyyMMddHHmmss}.csv";
                    string sql = "";

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
                            string whereSqlComputadores = whereClauses.Any() ? $"WHERE {string.Join(" AND ", whereClauses)}" : "";
                            sql = $"SELECT {computerHeader} FROM Computadores {whereSqlComputadores}";

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
                            string whereSqlMonitores = whereClauses.Any() ? $"WHERE {string.Join(" AND ", whereClauses)}" : "";
                            sql = $"SELECT {monitorHeader} FROM Monitores {whereSqlMonitores}";

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

                            string perifericoHeader = "ID,ColaboradorNome,Tipo,DataEntrega,PartNumber";
                            csvBuilder.AppendLine(perifericoHeader);
                            string whereSqlPerifericos = whereClauses.Any() ? $"WHERE {string.Join(" AND ", whereClauses)}" : "";
                            sql = $"SELECT {perifericoHeader} FROM Perifericos {whereSqlPerifericos}";

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

                    var whereSql = "WHERE ColaboradorNome = @colaborador";
                    parameters.Add("@colaborador", viewModel.ColaboradorNome);

                    // Computadores
                    csvBuilder.AppendLine("Computadores");
                    csvBuilder.AppendLine("MAC,IP,Hostname,Fabricante,Processador,SO,DataColeta");
                    string sqlComputadores = $"SELECT MAC, IP, Hostname, Fabricante, Processador, SO, DataColeta FROM Computadores {whereSql}";
                    using (var cmd = new SqlCommand(sqlComputadores, connection))
                    {
                        foreach (var p in parameters)
                        {
                            cmd.Parameters.AddWithValue(p.Key, p.Value);
                        }
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
                    string sqlMonitores = $"SELECT PartNumber, ColaboradorNome, Marca, Modelo, Tamanho FROM Monitores {whereSql}";
                    using (var cmd = new SqlCommand(sqlMonitores, connection))
                    {
                        foreach (var p in parameters)
                        {
                            cmd.Parameters.AddWithValue(p.Key, p.Value);
                        }
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
                    csvBuilder.AppendLine("ID,ColaboradorNome,Tipo,DataEntrega,PartNumber");
                    string sqlPerifericos = $"SELECT ID, ColaboradorNome, Tipo, DataEntrega, PartNumber FROM Perifericos {whereSql}";
                    using (var cmd = new SqlCommand(sqlPerifericos, connection))
                    {
                        foreach (var p in parameters)
                        {
                            cmd.Parameters.AddWithValue(p.Key, p.Value);
                        }
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                csvBuilder.AppendLine($"{reader["ID"]},{reader["ColaboradorNome"]},{reader["Tipo"]},{reader["DataEntrega"]},{reader["PartNumber"]}");
                            }
                        }
                    }
                }
            }

            return File(Encoding.UTF8.GetBytes(csvBuilder.ToString()), "text/csv", fileName);
        }
    }
}
