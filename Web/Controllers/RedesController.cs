using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Web.Models;
using System.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Web.Services;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class RedesController : Controller
    {
        private readonly string _connectionString;
        private readonly ILogger<RedesController> _logger;
        private readonly PersistentLogService _persistentLogService;

        public RedesController(IConfiguration configuration, ILogger<RedesController> logger, PersistentLogService persistentLogService)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
            _persistentLogService = persistentLogService;
        }

        public IActionResult Index()
        {
            var redes = new List<Rede>();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var command = new SqlCommand("SELECT * FROM Rede", connection);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            redes.Add(new Rede
                            {
                                Id = (int)reader["Id"],
                                Tipo = reader["Tipo"].ToString(),
                                IP = reader["IP"].ToString(),
                                MAC = reader["MAC"].ToString(),
                                Nome = reader["Nome"].ToString(),
                                DataInclusao = (DateTime)reader["DataInclusao"],
                                DataAlteracao = reader["DataAlteracao"] as DateTime?,
                                Observacao = reader["Observacao"].ToString()
                            });
                        }
                    }
                }

                // Sort the list in-memory using System.Version for correct IP sorting
                var redesOrdenadas = redes.OrderBy(r => Version.Parse(r.IP)).ToList();
                return View(redesOrdenadas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting network assets list.");
                // Handle error appropriately
                return View(redes); // Return unsorted list in case of an error
            }
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Rede rede)
        {
            _logger.LogInformation("Create POST action called for network asset.");
            if (ModelState.IsValid)
            {
                _logger.LogInformation("ModelState is valid. Attempting to save to the database.");
                try
                {
                    using (var connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();
                        var command = new SqlCommand("INSERT INTO Rede (Tipo, IP, MAC, Nome, DataInclusao, Observacao) VALUES (@Tipo, @IP, @MAC, @Nome, @DataInclusao, @Observacao)", connection);
                        command.Parameters.AddWithValue("@Tipo", rede.Tipo);
                        command.Parameters.AddWithValue("@IP", rede.IP);
                        command.Parameters.AddWithValue("@MAC", (object)rede.MAC ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Nome", rede.Nome);
                        command.Parameters.AddWithValue("@DataInclusao", DateTime.Now);
                        command.Parameters.AddWithValue("@Observacao", (object)rede.Observacao ?? DBNull.Value);

                        _logger.LogInformation("Executing INSERT command for network asset '{Nome}'.", rede.Nome);
                        command.ExecuteNonQuery();
                        _logger.LogInformation("INSERT command executed successfully.");
                    }
                    _persistentLogService.AddLog("Rede", "Create", User.Identity.Name, $"Network asset '{rede.Nome}' created.");
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating network asset in the database.");
                    ModelState.AddModelError(string.Empty, "Ocorreu um erro ao salvar os dados. Por favor, tente novamente.");
                }
            }
            else
            {
                _logger.LogWarning("ModelState is invalid. Validation errors:");
                foreach (var state in ModelState)
                {
                    if (state.Value.Errors.Any())
                    {
                        var errors = string.Join(", ", state.Value.Errors.Select(e => e.ErrorMessage));
                        _logger.LogWarning($"- {state.Key}: {errors}");
                    }
                }
            }
            _logger.LogInformation("Returning view with model due to validation errors or exception.");
            return View(rede);
        }

        public IActionResult Edit(int id)
        {
            var rede = FindRedeById(id);
            if (rede == null)
            {
                return NotFound();
            }
            return View(rede);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Rede rede)
        {
            if (id != rede.Id)
            {
                return NotFound();
            }

            _logger.LogInformation("Edit POST action called for network asset ID {Id}.", id);
            if (ModelState.IsValid)
            {
                _logger.LogInformation("ModelState is valid. Attempting to update the database.");
                try
                {
                    using (var connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();
                        var command = new SqlCommand("UPDATE Rede SET Tipo = @Tipo, IP = @IP, MAC = @MAC, Nome = @Nome, DataAlteracao = @DataAlteracao, Observacao = @Observacao WHERE Id = @Id", connection);
                        command.Parameters.AddWithValue("@Id", rede.Id);
                        command.Parameters.AddWithValue("@Tipo", rede.Tipo);
                        command.Parameters.AddWithValue("@IP", rede.IP);
                        command.Parameters.AddWithValue("@MAC", (object)rede.MAC ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Nome", rede.Nome);
                        command.Parameters.AddWithValue("@DataAlteracao", DateTime.Now);
                        command.Parameters.AddWithValue("@Observacao", (object)rede.Observacao ?? DBNull.Value);

                        _logger.LogInformation("Executing UPDATE command for network asset ID {Id}.", rede.Id);
                        command.ExecuteNonQuery();
                        _logger.LogInformation("UPDATE command executed successfully for ID {Id}.", rede.Id);
                    }
                    _persistentLogService.AddLog("Rede", "Update", User.Identity.Name, $"Network asset '{rede.Nome}' updated.");
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating network asset in the database.");
                    ModelState.AddModelError(string.Empty, "Ocorreu um erro ao salvar os dados. Por favor, tente novamente.");
                }
            }
            else
            {
                _logger.LogWarning("ModelState is invalid. Validation errors:");
                foreach (var state in ModelState)
                {
                    if (state.Value.Errors.Any())
                    {
                        var errors = string.Join(", ", state.Value.Errors.Select(e => e.ErrorMessage));
                        _logger.LogWarning($"- {state.Key}: {errors}");
                    }
                }
            }
            _logger.LogInformation("Returning view with model due to validation errors or exception.");
            return View(rede);
        }

        public IActionResult Delete(int id)
        {
            var rede = FindRedeById(id);
            if (rede == null)
            {
                return NotFound();
            }
            return View(rede);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var command = new SqlCommand("DELETE FROM Rede WHERE Id = @Id", connection);
                    command.Parameters.AddWithValue("@Id", id);
                    command.ExecuteNonQuery();
                }
                _persistentLogService.AddLog("Rede", "Delete", User.Identity.Name, $"Network asset with id '{id}' deleted.");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting network asset.");
                // Handle error
            }
            return RedirectToAction(nameof(Index));
        }

        private Rede FindRedeById(int id)
        {
            Rede rede = null;
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var command = new SqlCommand("SELECT * FROM Rede WHERE Id = @Id", connection);
                    command.Parameters.AddWithValue("@Id", id);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            rede = new Rede
                            {
                                Id = (int)reader["Id"],
                                Tipo = reader["Tipo"].ToString(),
                                IP = reader["IP"].ToString(),
                                MAC = reader["MAC"].ToString(),
                                Nome = reader["Nome"].ToString(),
                                DataInclusao = (DateTime)reader["DataInclusao"],
                                DataAlteracao = reader["DataAlteracao"] as DateTime?,
                                Observacao = reader["Observacao"].ToString()
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding network asset by id.");
                // Handle error
            }
            return rede;
        }
    }
}
