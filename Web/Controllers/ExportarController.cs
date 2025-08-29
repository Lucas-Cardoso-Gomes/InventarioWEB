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
    [Authorize(Roles = "Admin,Supervisor")]
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

            if (User.IsInRole("Admin") || User.IsInRole("Diretoria") || User.IsInRole("RH"))
            {
                viewModel.AllSupervisores = allUsers.Where(u => u.Role == "Supervisor" || u.Role == "Admin").Select(u => u.Nome).ToList();
            }

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Export(ExportarViewModel viewModel)
        {
            var csvBuilder = new StringBuilder();
            string fileName = $"export_{DateTime.Now:yyyyMMddHHmmss}.csv";

            var allUsers = await _userService.GetAllUsersAsync();

            if (viewModel.ExportMode == ExportMode.PorDispositivo)
            {
                fileName = $"export_{viewModel.DeviceType}_{DateTime.Now:yyyyMMddHHmmss}.csv";
                AppendAllDevicesToCsv(csvBuilder, viewModel.DeviceType);
            }
            else if (viewModel.ExportMode == ExportMode.PorColaborador)
            {
                fileName = $"export_colaborador_{viewModel.ColaboradorNome}_{DateTime.Now:yyyyMMddHHmmss}.csv";

                var user = allUsers.FirstOrDefault(u => u.Nome == viewModel.ColaboradorNome);
                if (user != null)
                {
                    csvBuilder.AppendLine("Usuario");
                    csvBuilder.AppendLine("Nome,Email,Departamento,Supervisor");
                    var supervisorName = user.SupervisorId.HasValue ? allUsers.FirstOrDefault(sup => sup.Id == user.SupervisorId.Value)?.Nome : "";
                    csvBuilder.AppendLine($"{user.Nome},{user.Email},{user.Departamento},{supervisorName}");

                    AppendDevicesToCsv(csvBuilder, new List<User> { user });
                }
            }
            else if (viewModel.ExportMode == ExportMode.PorSupervisor)
            {
                fileName = $"export_por_supervisor_{DateTime.Now:yyyyMMddHHmmss}.csv";
                var usersToExport = new List<User>();
                var selectedSupervisores = new List<string>();

                if (User.IsInRole("Admin") || User.IsInRole("Diretoria") || User.IsInRole("RH"))
                {
                    selectedSupervisores = viewModel.SelectedSupervisores;
                    if (selectedSupervisores.Any())
                    {
                        var supervisorIds = allUsers.Where(u => selectedSupervisores.Contains(u.Nome) && (u.Role == "Supervisor" || u.Role == "Admin")).Select(u => u.Id);
                        usersToExport = allUsers.Where(u => u.SupervisorId.HasValue && supervisorIds.Contains(u.SupervisorId.Value)).ToList();
                        if (viewModel.IncluirSupervisor)
                        {
                            usersToExport.AddRange(allUsers.Where(u => supervisorIds.Contains(u.Id)));
                        }
                    }
                }
                else if (User.IsInRole("Supervisor"))
                {
                    var currentUser = await _userService.FindByLoginAsync(User.Identity.Name);
                    usersToExport = allUsers.Where(u => u.SupervisorId == currentUser.Id).ToList();
                    if (viewModel.IncluirSupervisor)
                    {
                        usersToExport.Add(currentUser);
                    }
                }

                csvBuilder.AppendLine("Usuarios");
                csvBuilder.AppendLine("Nome,Email,Departamento,Supervisor");
                foreach (var u in usersToExport)
                {
                    var supervisorName = u.SupervisorId.HasValue ? allUsers.FirstOrDefault(sup => sup.Id == u.SupervisorId.Value)?.Nome : "";
                    csvBuilder.AppendLine($"{u.Nome},{u.Email},{u.Departamento},{supervisorName}");
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
        private void AppendAllDevicesToCsv(StringBuilder csvBuilder, string deviceType)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                switch (deviceType)
                {
                    case "Computadores":
                        csvBuilder.AppendLine("Computadores");
                        csvBuilder.AppendLine("MAC,IP,Colaborador,Hostname,Fabricante,Processador,SO,DataColeta");
                        var sqlComputadores = "SELECT c.MAC, c.IP, u.Nome as ColaboradorNome, c.Hostname, c.Fabricante, c.Processador, c.SO, c.DataColeta FROM Computadores c LEFT JOIN Users u ON c.UserId = u.Id";
                        using (var cmd = new SqlCommand(sqlComputadores, connection))
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                csvBuilder.AppendLine($"{reader["MAC"]},{reader["IP"]},{reader["ColaboradorNome"]},{reader["Hostname"]},{reader["Fabricante"]},{reader["Processador"]},{reader["SO"]},{reader["DataColeta"]}");
                            }
                        }
                        break;
                    case "Monitores":
                        csvBuilder.AppendLine("Monitores");
                        csvBuilder.AppendLine("PartNumber,Colaborador,Marca,Modelo,Tamanho");
                        var sqlMonitores = "SELECT m.PartNumber, u.Nome as ColaboradorNome, m.Marca, m.Modelo, m.Tamanho FROM Monitores m LEFT JOIN Users u ON m.UserId = u.Id";
                        using (var cmd = new SqlCommand(sqlMonitores, connection))
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                csvBuilder.AppendLine($"{reader["PartNumber"]},{reader["ColaboradorNome"]},{reader["Marca"]},{reader["Modelo"]},{reader["Tamanho"]}");
                            }
                        }
                        break;
                    case "Perifericos":
                        csvBuilder.AppendLine("Perifericos");
                        csvBuilder.AppendLine("ID,Colaborador,Tipo,DataEntrega,PartNumber");
                        var sqlPerifericos = "SELECT p.ID, u.Nome as ColaboradorNome, p.Tipo, p.DataEntrega, p.PartNumber FROM Perifericos p LEFT JOIN Users u ON p.UserId = u.Id";
                        using (var cmd = new SqlCommand(sqlPerifericos, connection))
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                csvBuilder.AppendLine($"{reader["ID"]},{reader["ColaboradorNome"]},{reader["Tipo"]},{reader["DataEntrega"]},{reader["PartNumber"]}");
                            }
                        }
                        break;
                }
            }
        }
    }
}
