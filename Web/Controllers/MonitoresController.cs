using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using web.Models;
using Web.Services;
using Monitor = web.Models.Monitor;
using System.Threading.Tasks;
using System.Linq;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin,Coordenador,Normal")]
    public class MonitoresController : Controller
    {
        private readonly string _connectionString;
        private readonly ILogger<MonitoresController> _logger;
        private readonly PersistentLogService _persistentLogService;
        private readonly UserService _userService;

        public MonitoresController(IConfiguration configuration, ILogger<MonitoresController> logger, PersistentLogService persistentLogService, UserService userService)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
            _persistentLogService = persistentLogService;
            _userService = userService;
        }

        public async Task<IActionResult> Index(List<string> currentMarcas, List<string> currentTamanhos, List<string> currentModelos)
        {
            var viewModel = new MonitorIndexViewModel
            {
                CurrentMarcas = currentMarcas,
                CurrentTamanhos = currentTamanhos,
                CurrentModelos = currentModelos,
                Monitores = new List<Monitor>()
            };

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    viewModel.Marcas = GetDistinctMonitorValues(connection, "Marca");
                    viewModel.Tamanhos = GetDistinctMonitorValues(connection, "Tamanho");
                    viewModel.Modelos = GetDistinctMonitorValues(connection, "Modelo");

                    var whereClauses = new List<string>();
                    var parameters = new Dictionary<string, object>();

                    Action<string, List<string>> addInClause = (columnName, values) =>
                    {
                        if (values != null && values.Any())
                        {
                            var paramNames = new List<string>();
                            for (int i = 0; i < values.Count; i++)
                            {
                                var paramName = $"@{columnName.ToLower()}{i}";
                                paramNames.Add(paramName);
                                parameters.Add(paramName, values[i]);
                            }
                            whereClauses.Add($"{columnName} IN ({string.Join(", ", paramNames)})");
                        }
                    };

                    addInClause("Marca", currentMarcas);
                    addInClause("Tamanho", currentTamanhos);
                    addInClause("Modelo", currentModelos);

                    var user = await _userService.FindByLoginAsync(User.Identity.Name);
                    if (User.IsInRole("Coordenador"))
                    {
                        var users = await _userService.GetUsersBySupervisorAsync(user.Id);
                        var userIds = users.Select(u => u.Id).ToList();
                        userIds.Add(user.Id);

                        var idParams = new List<string>();
                        for (int i = 0; i < userIds.Count; i++)
                        {
                            var paramName = $"@userId{i}";
                            idParams.Add(paramName);
                            parameters.Add(paramName, userIds[i]);
                        }
                        whereClauses.Add($"UserId IN ({string.Join(", ", idParams)})");
                    }

                    string whereSql = whereClauses.Any() ? $"WHERE {string.Join(" AND ", whereClauses)}" : "";

                    string sql = $"SELECT * FROM Monitores {whereSql}";

                    using (SqlCommand cmd = new SqlCommand(sql, connection))
                    {
                        foreach (var p in parameters)
                        {
                            cmd.Parameters.AddWithValue(p.Key, p.Value);
                        }

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                viewModel.Monitores.Add(new Monitor
                                {
                                    PartNumber = reader["PartNumber"].ToString(),
                                    ColaboradorNome = reader["ColaboradorNome"].ToString(),
                                    Marca = reader["Marca"].ToString(),
                                    Modelo = reader["Modelo"].ToString(),
                                    Tamanho = reader["Tamanho"].ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter a lista de monitores.");
            }
            return View(viewModel);
        }

        private List<string> GetDistinctMonitorValues(SqlConnection connection, string columnName)
        {
            var values = new List<string>();
            using (var command = new SqlCommand($"SELECT DISTINCT {columnName} FROM Monitores WHERE {columnName} IS NOT NULL ORDER BY {columnName}", connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        values.Add(reader.GetString(0));
                    }
                }
            }
            return values;
        }

        // GET: Monitores/Create
        [Authorize(Roles = "Admin,Coordenador")]
        public async Task<IActionResult> Create()
        {
            ViewData["UserId"] = new SelectList(await _userService.GetAllUsersAsync(), "Id", "Nome");
            return View();
        }

        // POST: Monitores/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Coordenador")]
        public async Task<IActionResult> Create(Monitor monitor)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var user = await _userService.FindByIdAsync(monitor.UserId.Value);
                    monitor.ColaboradorNome = user?.Nome;

                    using (SqlConnection connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();
                        string sql = "INSERT INTO Monitores (PartNumber, UserId, ColaboradorNome, Marca, Modelo, Tamanho) VALUES (@PartNumber, @UserId, @ColaboradorNome, @Marca, @Modelo, @Tamanho)";
                        using (SqlCommand cmd = new SqlCommand(sql, connection))
                        {
                            cmd.Parameters.AddWithValue("@PartNumber", monitor.PartNumber);
                            cmd.Parameters.AddWithValue("@UserId", (object)monitor.UserId ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ColaboradorNome", (object)monitor.ColaboradorNome ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Marca", (object)monitor.Marca ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Modelo", (object)monitor.Modelo ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Tamanho", (object)monitor.Tamanho ?? DBNull.Value);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    _persistentLogService.AddLog("Monitor", "Create", User.Identity.Name, $"Monitor '{monitor.PartNumber}' created.");
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao criar monitor.");
                    ModelState.AddModelError(string.Empty, "Ocorreu um erro ao criar o monitor. Verifique se o PartNumber já existe.");
                }
            }
            ViewData["UserId"] = new SelectList(await _userService.GetAllUsersAsync(), "Id", "Nome", monitor.UserId);
            return View(monitor);
        }

        // GET: Monitores/Edit/5
        [Authorize(Roles = "Admin,Coordenador")]
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null) return NotFound();
            Monitor monitor = FindMonitorById(id);
            if (monitor == null) return NotFound();
            ViewData["UserId"] = new SelectList(await _userService.GetAllUsersAsync(), "Id", "Nome", monitor.UserId);
            return View(monitor);
        }

        // POST: Monitores/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Coordenador")]
        public async Task<IActionResult> Edit(string id, Monitor monitor)
        {
            if (id != monitor.PartNumber) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var user = await _userService.FindByIdAsync(monitor.UserId.Value);
                    monitor.ColaboradorNome = user?.Nome;

                    using (SqlConnection connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();
                        string sql = "UPDATE Monitores SET UserId = @UserId, ColaboradorNome = @ColaboradorNome, Marca = @Marca, Modelo = @Modelo, Tamanho = @Tamanho WHERE PartNumber = @PartNumber";
                        using (SqlCommand cmd = new SqlCommand(sql, connection))
                        {
                            cmd.Parameters.AddWithValue("@PartNumber", monitor.PartNumber);
                            cmd.Parameters.AddWithValue("@UserId", (object)monitor.UserId ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ColaboradorNome", (object)monitor.ColaboradorNome ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Marca", (object)monitor.Marca ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Modelo", (object)monitor.Modelo ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Tamanho", (object)monitor.Tamanho ?? DBNull.Value);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    _persistentLogService.AddLog("Monitor", "Update", User.Identity.Name, $"Monitor '{monitor.PartNumber}' updated.");
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao editar monitor.");
                    ModelState.AddModelError(string.Empty, "Ocorreu um erro ao editar o monitor.");
                }
            }
            ViewData["UserId"] = new SelectList(await _userService.GetAllUsersAsync(), "Id", "Nome", monitor.UserId);
            return View(monitor);
        }

        // GET: Monitores/Delete/5
        [Authorize(Roles = "Admin,Coordenador")]
        public IActionResult Delete(string id)
        {
            if (id == null) return NotFound();
            Monitor monitor = FindMonitorById(id);
            if (monitor == null) return NotFound();
            return View(monitor);
        }

        // POST: Monitores/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Coordenador")]
        public IActionResult DeleteConfirmed(string id)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    string sql = "DELETE FROM Monitores WHERE PartNumber = @PartNumber";
                    using (SqlCommand cmd = new SqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@PartNumber", id);
                        cmd.ExecuteNonQuery();
                    }
                }
                _persistentLogService.AddLog("Monitor", "Delete", User.Identity.Name, $"Monitor '{id}' deleted.");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao excluir monitor.");
                ViewBag.ErrorMessage = "Ocorreu um erro ao excluir o monitor.";
                return View(FindMonitorById(id));
            }
        }

        private Monitor FindMonitorById(string id)
        {
            Monitor monitor = null;
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string sql = "SELECT * FROM Monitores WHERE PartNumber = @PartNumber";
                using (SqlCommand cmd = new SqlCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@PartNumber", id);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            monitor = new Monitor
                            {
                                PartNumber = reader["PartNumber"].ToString(),
                                ColaboradorNome = reader["ColaboradorNome"].ToString(),
                                Marca = reader["Marca"].ToString(),
                                Modelo = reader["Modelo"].ToString(),
                                Tamanho = reader["Tamanho"].ToString()
                            };
                        }
                    }
                }
            }
            return monitor;
        }

    }
}
