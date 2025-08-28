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

            if (User.IsInRole("Admin") || User.IsInRole("Diretoria") || User.IsInRole("RH"))
            {
                viewModel.AllCoordenadores = allUsers.Where(u => u.Role == "Coordenador").Select(u => u.Nome).ToList();
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
                // ... (logic for exporting by device)
            }
            else if (viewModel.ExportMode == ExportMode.PorColaborador)
            {
                fileName = $"export_colaborador_{viewModel.ColaboradorNome}_{DateTime.Now:yyyyMMddHHmmss}.csv";

                var user = allUsers.FirstOrDefault(u => u.Nome == viewModel.ColaboradorNome);
                if (user != null)
                {
                    csvBuilder.AppendLine("Usuario");
                    csvBuilder.AppendLine("Nome,CPF,Email,Setor,Supervisor");
                    var supervisorName = user.SupervisorId.HasValue ? allUsers.FirstOrDefault(sup => sup.Id == user.SupervisorId.Value)?.Nome : "";
                    csvBuilder.AppendLine($"{user.Nome},{user.CPF},{user.Email},{user.Setor},{supervisorName}");

                    AppendDevicesToCsv(csvBuilder, new List<User> { user });
                }
            }
            else if (viewModel.ExportMode == ExportMode.PorCoordenador)
            {
                fileName = $"export_por_coordenador_{DateTime.Now:yyyyMMddHHmmss}.csv";
                var usersToExport = new List<User>();
                var selectedCoordenadores = new List<string>();

                if (User.IsInRole("Admin") || User.IsInRole("Diretoria") || User.IsInRole("RH"))
                {
                    selectedCoordenadores = viewModel.SelectedCoordenadores;
                    if (selectedCoordenadores.Any())
                    {
                        var coordinatorIds = allUsers.Where(u => selectedCoordenadores.Contains(u.Nome) && u.Role == "Coordenador").Select(u => u.Id);
                        usersToExport = allUsers.Where(u => u.SupervisorId.HasValue && coordinatorIds.Contains(u.SupervisorId.Value)).ToList();
                        if(viewModel.IncluirCoordenador)
                        {
                            usersToExport.AddRange(allUsers.Where(u => coordinatorIds.Contains(u.Id)));
                        }
                    }
                }
                else if (User.IsInRole("Coordenador"))
                {
                    var currentUser = await _userService.FindByLoginAsync(User.Identity.Name);
                    usersToExport = allUsers.Where(u => u.SupervisorId == currentUser.Id).ToList();
                    if(viewModel.IncluirCoordenador)
                    {
                        usersToExport.Add(currentUser);
                    }
                }

                csvBuilder.AppendLine("Usuarios");
                csvBuilder.AppendLine("Nome,CPF,Email,Setor,Supervisor");
                foreach (var u in usersToExport)
                {
                    var supervisorName = u.SupervisorId.HasValue ? allUsers.FirstOrDefault(sup => sup.Id == u.SupervisorId.Value)?.Nome : "";
                    csvBuilder.AppendLine($"{u.Nome},{u.CPF},{u.Email},{u.Setor},{supervisorName}");
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
                var whereClause = $"WHERE UserId IN ({string.Join(", ", idParams)})";

                // Computadores
                csvBuilder.AppendLine();
                csvBuilder.AppendLine("Computadores");
                csvBuilder.AppendLine("MAC,IP,ColaboradorNome,Hostname,Fabricante,Processador,SO,DataColeta");
                var sqlComputadores = $"SELECT MAC, IP, ColaboradorNome, Hostname, Fabricante, Processador, SO, DataColeta FROM Computadores {whereClause}";
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
                csvBuilder.AppendLine();
                csvBuilder.AppendLine("Monitores");
                csvBuilder.AppendLine("PartNumber,ColaboradorNome,Marca,Modelo,Tamanho");
                var sqlMonitores = $"SELECT PartNumber, ColaboradorNome, Marca, Modelo, Tamanho FROM Monitores {whereClause}";
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
                csvBuilder.AppendLine();
                csvBuilder.AppendLine("Perifericos");
                csvBuilder.AppendLine("ID,ColaboradorNome,Tipo,DataEntrega,PartNumber");
                var sqlPerifericos = $"SELECT ID, ColaboradorNome, Tipo, DataEntrega, PartNumber FROM Perifericos {whereClause}";
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
    }
}
