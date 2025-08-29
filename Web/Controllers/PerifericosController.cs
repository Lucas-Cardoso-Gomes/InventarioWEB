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
using System.Threading.Tasks;
using System.Linq;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin,Coordenador,Normal")]
    public class PerifericosController : Controller
    {
        private readonly string _connectionString;
        private readonly ILogger<PerifericosController> _logger;
        private readonly PersistentLogService _persistentLogService;
        private readonly UserService _userService;

        public PerifericosController(IConfiguration configuration, ILogger<PerifericosController> logger, PersistentLogService persistentLogService, UserService userService)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
            _persistentLogService = persistentLogService;
            _userService = userService;
        }

        // GET: Perifericos
        public async Task<IActionResult> Index(string searchString)
        {
            ViewData["CurrentFilter"] = searchString;
            var perifericos = new List<Periferico>();
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var whereClauses = new List<string>();
                    var parameters = new Dictionary<string, object>();

                    if (!string.IsNullOrEmpty(searchString))
                    {
                        whereClauses.Add("(u.Nome LIKE @search OR p.Tipo LIKE @search OR p.PartNumber LIKE @search)");
                        parameters.Add("@search", $"%{searchString}%");
                    }

                    var user = await _userService.FindByLoginAsync(User.Identity.Name);
                    if (User.IsInRole("Coordenador"))
                    {
                        var users = await _userService.GetUsersByCoordenadorAsync(user.Id);
                        var userIds = users.Select(u => u.Id).ToList();
                        userIds.Add(user.Id);

                        var idParams = new List<string>();
                        for (int i = 0; i < userIds.Count; i++)
                        {
                            var paramName = $"@userId{i}";
                            idParams.Add(paramName);
                            parameters.Add(paramName, userIds[i]);
                        }
                        whereClauses.Add($"p.UserId IN ({string.Join(", ", idParams)})");
                    }

                    string whereSql = whereClauses.Any() ? $"WHERE {string.Join(" AND ", whereClauses)}" : "";
                    string sql = $"SELECT p.ID, p.UserId, u.Nome as ColaboradorNome, p.Tipo, p.DataEntrega, p.PartNumber FROM Perifericos p LEFT JOIN Users u ON p.UserId = u.Id {whereSql}";

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
                                perifericos.Add(new Periferico
                                {
                                    ID = Convert.ToInt32(reader["ID"]),
                                    UserId = reader["UserId"] != DBNull.Value ? Convert.ToInt32(reader["UserId"]) : (int?)null,
                                    ColaboradorNome = reader["ColaboradorNome"].ToString(),
                                    Tipo = reader["Tipo"].ToString(),
                                    DataEntrega = reader["DataEntrega"] != DBNull.Value ? Convert.ToDateTime(reader["DataEntrega"]) : (DateTime?)null,
                                    PartNumber = reader["PartNumber"].ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter a lista de periféricos.");
            }
            return View(perifericos);
        }

        // GET: Perifericos/Create
        [Authorize(Roles = "Admin,Coordenador")]
        public async Task<IActionResult> Create()
        {
            ViewData["UserId"] = new SelectList(await _userService.GetAllUsersAsync(), "Id", "Nome");
            return View();
        }

        // POST: Perifericos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Coordenador")]
        public async Task<IActionResult> Create(Periferico periferico)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (periferico.UserId.HasValue)
                    {
                        var user = await _userService.FindByIdAsync(periferico.UserId.Value);
                        periferico.ColaboradorNome = user?.Nome;
                    }
                    else
                    {
                        periferico.ColaboradorNome = null;
                    }

                    using (SqlConnection connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();
                        string sql = "INSERT INTO Perifericos (UserId, Tipo, DataEntrega, PartNumber) VALUES (@UserId, @Tipo, @DataEntrega, @PartNumber)";
                        using (SqlCommand cmd = new SqlCommand(sql, connection))
                        {
                            cmd.Parameters.AddWithValue("@UserId", (object)periferico.UserId ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Tipo", periferico.Tipo);
                            cmd.Parameters.AddWithValue("@DataEntrega", (object)periferico.DataEntrega ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@PartNumber", (object)periferico.PartNumber ?? DBNull.Value);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    _persistentLogService.AddLog("Periferico", "Create", User.Identity.Name, $"Peripheral '{periferico.Tipo} - {periferico.PartNumber}' created.");
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao criar periférico.");
                    ModelState.AddModelError(string.Empty, "Ocorreu um erro ao criar o periférico.");
                }
            }
            ViewData["UserId"] = new SelectList(await _userService.GetAllUsersAsync(), "Id", "Nome", periferico.UserId);
            return View(periferico);
        }

        // GET: Perifericos/Edit/5
        [Authorize(Roles = "Admin,Coordenador")]
        public async Task<IActionResult> Edit(int id)
        {
            Periferico periferico = FindPerifericoById(id);
            if (periferico == null) return NotFound();
            ViewData["UserId"] = new SelectList(await _userService.GetAllUsersAsync(), "Id", "Nome", periferico.UserId);
            return View(periferico);
        }

        // POST: Perifericos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Coordenador")]
        public async Task<IActionResult> Edit(int id, Periferico periferico)
        {
            if (id != periferico.ID) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    if (periferico.UserId.HasValue)
                    {
                        var user = await _userService.FindByIdAsync(periferico.UserId.Value);
                        periferico.ColaboradorNome = user?.Nome;
                    }
                    else
                    {
                        periferico.ColaboradorNome = null;
                    }

                    using (SqlConnection connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();
                        string sql = "UPDATE Perifericos SET UserId = @UserId, Tipo = @Tipo, DataEntrega = @DataEntrega, PartNumber = @PartNumber WHERE ID = @ID";
                        using (SqlCommand cmd = new SqlCommand(sql, connection))
                        {
                            cmd.Parameters.AddWithValue("@ID", periferico.ID);
                            cmd.Parameters.AddWithValue("@UserId", (object)periferico.UserId ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Tipo", periferico.Tipo);
                            cmd.Parameters.AddWithValue("@DataEntrega", (object)periferico.DataEntrega ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@PartNumber", (object)periferico.PartNumber ?? DBNull.Value);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    _persistentLogService.AddLog("Periferico", "Update", User.Identity.Name, $"Peripheral '{periferico.Tipo} - {periferico.PartNumber}' updated.");
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao editar periférico.");
                    ModelState.AddModelError(string.Empty, "Ocorreu um erro ao editar o periférico.");
                }
            }
            ViewData["UserId"] = new SelectList(await _userService.GetAllUsersAsync(), "Id", "Nome", periferico.UserId);
            return View(periferico);
        }

        // GET: Perifericos/Delete/5
        [Authorize(Roles = "Admin,Coordenador")]
        public IActionResult Delete(int id)
        {
            Periferico periferico = FindPerifericoById(id);
            if (periferico == null) return NotFound();
            return View(periferico);
        }

        // POST: Perifericos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Coordenador")]
        public IActionResult DeleteConfirmed(int id)
        {
            try
            {
                var periferico = FindPerifericoById(id);
                if (periferico != null)
                {
                    using (SqlConnection connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();
                        string sql = "DELETE FROM Perifericos WHERE ID = @ID";
                        using (SqlCommand cmd = new SqlCommand(sql, connection))
                        {
                            cmd.Parameters.AddWithValue("@ID", id);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    _persistentLogService.AddLog("Periferico", "Delete", User.Identity.Name, $"Peripheral '{periferico.Tipo} - {periferico.PartNumber}' deleted.");
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao excluir periférico.");
                ViewBag.ErrorMessage = "Ocorreu um erro ao excluir o periférico.";
                return View(FindPerifericoById(id));
            }
        }

        private Periferico FindPerifericoById(int id)
        {
            Periferico periferico = null;
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string sql = "SELECT p.ID, p.UserId, u.Nome as ColaboradorNome, p.Tipo, p.DataEntrega, p.PartNumber FROM Perifericos p LEFT JOIN Users u ON p.UserId = u.Id WHERE p.ID = @ID";
                using (SqlCommand cmd = new SqlCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@ID", id);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            periferico = new Periferico
                            {
                                ID = Convert.ToInt32(reader["ID"]),
                                UserId = reader["UserId"] != DBNull.Value ? Convert.ToInt32(reader["UserId"]) : (int?)null,
                                ColaboradorNome = reader["ColaboradorNome"].ToString(),
                                Tipo = reader["Tipo"].ToString(),
                                DataEntrega = reader["DataEntrega"] != DBNull.Value ? Convert.ToDateTime(reader["DataEntrega"]) : (DateTime?)null,
                                PartNumber = reader["PartNumber"].ToString()
                            };
                        }
                    }
                }
            }
            return periferico;
        }

    }
}
