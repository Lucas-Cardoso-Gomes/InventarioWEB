using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using web.Models;
using Web.Models;
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
            var allUsers = await _userService.GetAllUsersAsync();
            viewModel.Colaboradores = allUsers.Select(u => u.Nome).ToList();

            if (User.IsInRole("Admin"))
            {
                viewModel.AllCoordenadores = allUsers.Where(u => u.Role == "Coordenador" || u.Role == "Admin").Select(u => u.Nome).ToList();
            }

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                viewModel.Fabricantes = await GetDistinctValuesAsync(connection, "Computadores", "Fabricante");
                viewModel.SOs = await GetDistinctValuesAsync(connection, "Computadores", "SO");
                viewModel.ProcessadorFabricantes = await GetDistinctValuesAsync(connection, "Computadores", "ProcessadorFabricante");
                viewModel.RamTipos = await GetDistinctValuesAsync(connection, "Computadores", "RamTipo");
                viewModel.Processadores = await GetDistinctValuesAsync(connection, "Computadores", "Processador");
                viewModel.Rams = await GetDistinctValuesAsync(connection, "Computadores", "Ram");
                viewModel.Marcas = await GetDistinctValuesAsync(connection, "Monitores", "Marca");
                viewModel.Tamanhos = await GetDistinctValuesAsync(connection, "Monitores", "Tamanho");
                viewModel.Modelos = await GetDistinctValuesAsync(connection, "Monitores", "Modelo");
                viewModel.TiposPeriferico = await GetDistinctValuesAsync(connection, "Perifericos", "Tipo");
            }

            return View(viewModel);
        }

        private async Task<List<string>> GetDistinctValuesAsync(SqlConnection connection, string tableName, string columnName)
        {
            var values = new List<string>();
            var command = new SqlCommand($"SELECT DISTINCT {columnName} FROM {tableName} WHERE {columnName} IS NOT NULL ORDER BY {columnName}", connection);
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    values.Add(reader.GetString(0));
                }
            }
            return values;
        }

        [HttpPost]
        public async Task<IActionResult> Export(ExportarViewModel viewModel)
        {
            var csvBuilder = new StringBuilder();
            string fileName = $"export_{DateTime.Now:yyyyMMddHHmmss}.csv";

            var allUsers = await _userService.GetAllUsersAsync();
            var userNamesById = allUsers.ToDictionary(u => u.Id, u => u.Nome);

            if (viewModel.ExportMode == ExportMode.PorDispositivo)
            {
                fileName = $"export_{viewModel.DeviceType}_{DateTime.Now:yyyyMMddHHmmss}.csv";
                AppendAllDevicesToCsv(csvBuilder, viewModel);
            }
            else if (viewModel.ExportMode == ExportMode.PorColaborador)
            {
                fileName = $"export_colaborador_{viewModel.ColaboradorNome}_{DateTime.Now:yyyyMMddHHmmss}.csv";

                var user = allUsers.FirstOrDefault(u => u.Nome == viewModel.ColaboradorNome);
                if (user != null)
                {
                    csvBuilder.AppendLine("Usuario");
                    csvBuilder.AppendLine("Nome,Email,Setor,Coordenador");
                    var coordenadorName = user.CoordenadorId.HasValue ? userNamesById.GetValueOrDefault(user.CoordenadorId.Value, "") : "";
                    csvBuilder.AppendLine($"{user.Nome},{user.Email},{user.Setor},{coordenadorName}");

                    AppendDevicesToCsv(csvBuilder, new List<User> { user });
                }
            }
            else if (viewModel.ExportMode == ExportMode.PorCoordenador)
            {
                fileName = $"export_por_coordenador_{DateTime.Now:yyyyMMddHHmmss}.csv";
                var usersToExport = new List<User>();

                if (User.IsInRole("Admin"))
                {
                    if (viewModel.SelectedCoordenadores.Any())
                    {
                        var coordenadorIds = allUsers.Where(u => viewModel.SelectedCoordenadores.Contains(u.Nome) && (u.Role == "Coordenador" || u.Role == "Admin")).Select(u => u.Id);
                        usersToExport = allUsers.Where(u => u.CoordenadorId.HasValue && coordenadorIds.Contains(u.CoordenadorId.Value)).ToList();
                        if (viewModel.IncluirCoordenador)
                        {
                            usersToExport.AddRange(allUsers.Where(u => coordenadorIds.Contains(u.Id)));
                        }
                    }
                }
                else if (User.IsInRole("Coordenador"))
                {
                    var currentUser = allUsers.FirstOrDefault(u => u.Login == User.Identity.Name);
                    if (currentUser != null)
                    {
                        usersToExport = allUsers.Where(u => u.CoordenadorId == currentUser.Id).ToList();
                        if (viewModel.IncluirCoordenador)
                        {
                            usersToExport.Add(currentUser);
                        }
                    }
                }

                csvBuilder.AppendLine("Usuarios");
                csvBuilder.AppendLine("Nome,Email,Setor,Coordenador");
                foreach (var u in usersToExport)
                {
                    var coordenadorName = u.CoordenadorId.HasValue ? userNamesById.GetValueOrDefault(u.CoordenadorId.Value, "") : "";
                    csvBuilder.AppendLine($"{u.Nome},{u.Email},{u.Setor},{coordenadorName}");
                }

                AppendDevicesToCsv(csvBuilder, usersToExport);
            }

            return File(Encoding.UTF8.GetBytes(csvBuilder.ToString()), "text/csv", fileName);
        }

        private void AppendDevicesToCsv(StringBuilder csvBuilder, List<User> users)
        {
            var userIds = users.Select(u => u.Id).ToList();
            if (!userIds.Any()) return;

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var idParams = new List<string>();
                for (int i = 0; i < userIds.Count; i++)
                {
                    idParams.Add($"@userId{i}");
                }
                var whereClause = $"WHERE c.UserId IN ({string.Join(", ", idParams)})";

                // Computadores
                csvBuilder.AppendLine();
                csvBuilder.AppendLine("Computadores");
                csvBuilder.AppendLine("MAC,IP,Colaborador,Hostname,Fabricante,Processador,SO,DataColeta");
                var sqlComputadores = $"SELECT c.MAC, c.IP, u.Nome as ColaboradorNome, c.Hostname, c.Fabricante, c.Processador, c.SO, c.DataColeta FROM Computadores c LEFT JOIN Users u ON c.UserId = u.Id {whereClause}";
                using (var cmd = new SqlCommand(sqlComputadores, connection))
                {
                    for (int i = 0; i < userIds.Count; i++)
                    {
                        cmd.Parameters.AddWithValue($"@userId{i}", userIds[i]);
                    }
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            csvBuilder.AppendLine($"{reader["MAC"]},{reader["IP"]},{reader["ColaboradorNome"]},{reader["Hostname"]},{reader["Fabricante"]},{reader["Processador"]},{reader["SO"]},{reader["DataColeta"]}");
                        }
                    }
                }

                // Monitores
                var monWhereClause = $"WHERE m.UserId IN ({string.Join(", ", idParams)})";
                csvBuilder.AppendLine();
                csvBuilder.AppendLine("Monitores");
                csvBuilder.AppendLine("PartNumber,Colaborador,Marca,Modelo,Tamanho");
                var sqlMonitores = $"SELECT m.PartNumber, u.Nome as ColaboradorNome, m.Marca, m.Modelo, m.Tamanho FROM Monitores m LEFT JOIN Users u ON m.UserId = u.Id {monWhereClause}";
                using (var cmd = new SqlCommand(sqlMonitores, connection))
                {
                    for (int i = 0; i < userIds.Count; i++)
                    {
                        cmd.Parameters.AddWithValue($"@userId{i}", userIds[i]);
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
                var perWhereClause = $"WHERE p.UserId IN ({string.Join(", ", idParams)})";
                csvBuilder.AppendLine();
                csvBuilder.AppendLine("Perifericos");
                csvBuilder.AppendLine("ID,Colaborador,Tipo,DataEntrega,PartNumber");
                var sqlPerifericos = $"SELECT p.ID, u.Nome as ColaboradorNome, p.Tipo, p.DataEntrega, p.PartNumber FROM Perifericos p LEFT JOIN Users u ON p.UserId = u.Id {perWhereClause}";
                using (var cmd = new SqlCommand(sqlPerifericos, connection))
                {
                    for (int i = 0; i < userIds.Count; i++)
                    {
                        cmd.Parameters.AddWithValue($"@userId{i}", userIds[i]);
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
        private void AppendAllDevicesToCsv(StringBuilder csvBuilder, ExportarViewModel viewModel)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var whereClauses = new List<string>();
                var parameters = new Dictionary<string, object>();

                Action<string, List<string>> addInClause = (columnName, values) =>
                {
                    if (values != null && values.Any())
                    {
                        var paramNames = new List<string>();
                        for (int i = 0; i < values.Count; i++)
                        {
                            var paramName = $"@{columnName.ToLower().Replace(".", "")}{i}";
                            paramNames.Add(paramName);
                            parameters.Add(paramName, values[i]);
                        }
                        whereClauses.Add($"{columnName} IN ({string.Join(", ", paramNames)})");
                    }
                };

                switch (viewModel.DeviceType)
                {
                    case DeviceType.Computadores:
                        addInClause("c.Fabricante", viewModel.CurrentFabricantes);
                        addInClause("c.SO", viewModel.CurrentSOs);
                        addInClause("c.ProcessadorFabricante", viewModel.CurrentProcessadorFabricantes);
                        addInClause("c.RamTipo", viewModel.CurrentRamTipos);
                        addInClause("c.Processador", viewModel.CurrentProcessadores);
                        addInClause("c.Ram", viewModel.CurrentRams);

                        string whereSqlComputadores = whereClauses.Any() ? $"WHERE {string.Join(" AND ", whereClauses)}" : "";

                        csvBuilder.AppendLine("Computadores");
                        csvBuilder.AppendLine("MAC,IP,Colaborador,Hostname,Fabricante,Processador,SO,DataColeta");
                        var sqlComputadores = $"SELECT c.MAC, c.IP, u.Nome as ColaboradorNome, c.Hostname, c.Fabricante, c.Processador, c.SO, c.DataColeta FROM Computadores c LEFT JOIN Users u ON c.UserId = u.Id {whereSqlComputadores}";
                        using (var cmd = new SqlCommand(sqlComputadores, connection))
                        {
                            foreach(var p in parameters) cmd.Parameters.AddWithValue(p.Key, p.Value);
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    csvBuilder.AppendLine($"{reader["MAC"]},{reader["IP"]},{reader["ColaboradorNome"]},{reader["Hostname"]},{reader["Fabricante"]},{reader["Processador"]},{reader["SO"]},{reader["DataColeta"]}");
                                }
                            }
                        }
                        break;
                    case DeviceType.Monitores:
                        addInClause("m.Marca", viewModel.CurrentMarcas);
                        addInClause("m.Tamanho", viewModel.CurrentTamanhos);
                        addInClause("m.Modelo", viewModel.CurrentModelos);

                        string whereSqlMonitores = whereClauses.Any() ? $"WHERE {string.Join(" AND ", whereClauses)}" : "";

                        csvBuilder.AppendLine("Monitores");
                        csvBuilder.AppendLine("PartNumber,Colaborador,Marca,Modelo,Tamanho");
                        var sqlMonitores = $"SELECT m.PartNumber, u.Nome as ColaboradorNome, m.Marca, m.Modelo, m.Tamanho FROM Monitores m LEFT JOIN Users u ON m.UserId = u.Id {whereSqlMonitores}";
                        using (var cmd = new SqlCommand(sqlMonitores, connection))
                        {
                            foreach(var p in parameters) cmd.Parameters.AddWithValue(p.Key, p.Value);
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    csvBuilder.AppendLine($"{reader["PartNumber"]},{reader["ColaboradorNome"]},{reader["Marca"]},{reader["Modelo"]},{reader["Tamanho"]}");
                                }
                            }
                        }
                        break;
                    case DeviceType.Perifericos:
                        addInClause("p.Tipo", viewModel.CurrentTiposPeriferico);

                        string whereSqlPerifericos = whereClauses.Any() ? $"WHERE {string.Join(" AND ", whereClauses)}" : "";

                        csvBuilder.AppendLine("Perifericos");
                        csvBuilder.AppendLine("ID,Colaborador,Tipo,DataEntrega,PartNumber");
                        var sqlPerifericos = $"SELECT p.ID, u.Nome as ColaboradorNome, p.Tipo, p.DataEntrega, p.PartNumber FROM Perifericos p LEFT JOIN Users u ON p.UserId = u.Id {whereSqlPerifericos}";
                        using (var cmd = new SqlCommand(sqlPerifericos, connection))
                        {
                            foreach(var p in parameters) cmd.Parameters.AddWithValue(p.Key, p.Value);
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    csvBuilder.AppendLine($"{reader["ID"]},{reader["ColaboradorNome"]},{reader["Tipo"]},{reader["DataEntrega"]},{reader["PartNumber"]}");
                                }
                            }
                        }
                        break;
                }
            }
        }
    }
}
