using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using web.Models;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin,Coordenador,Normal")]
    public class MonitoresController : Controller
    {
        private readonly string _connectionString;
        private readonly ILogger<MonitoresController> _logger;

        public MonitoresController(IConfiguration configuration, ILogger<MonitoresController> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
        }

        // GET: Monitores
        public IActionResult Index(string searchString)
        {
            ViewData["CurrentFilter"] = searchString;
            var monitores = new List<Monitor>();
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    string sql = "SELECT * FROM Monitores";
                     if (!string.IsNullOrEmpty(searchString))
                    {
                        sql += " WHERE PartNumber LIKE @search OR ColaboradorNome LIKE @search OR Marca LIKE @search OR Modelo LIKE @search";
                    }
                    using (SqlCommand cmd = new SqlCommand(sql, connection))
                    {
                        if (!string.IsNullOrEmpty(searchString))
                        {
                            cmd.Parameters.AddWithValue("@search", $"%{searchString}%");
                        }
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                monitores.Add(new Monitor
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
            return View(monitores);
        }

        // GET: Monitores/Create
        [Authorize(Roles = "Admin,Coordenador")]
        public IActionResult Create()
        {
            ViewData["ColaboradorNome"] = new SelectList(GetColaboradores(), "Nome", "Nome");
            return View();
        }

        // POST: Monitores/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Coordenador")]
        public IActionResult Create(Monitor monitor)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();
                        string sql = "INSERT INTO Monitores (PartNumber, ColaboradorNome, Marca, Modelo, Tamanho) VALUES (@PartNumber, @ColaboradorNome, @Marca, @Modelo, @Tamanho)";
                        using (SqlCommand cmd = new SqlCommand(sql, connection))
                        {
                            cmd.Parameters.AddWithValue("@PartNumber", monitor.PartNumber);
                            cmd.Parameters.AddWithValue("@ColaboradorNome", (object)monitor.ColaboradorNome ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Marca", (object)monitor.Marca ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Modelo", (object)monitor.Modelo ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Tamanho", (object)monitor.Tamanho ?? DBNull.Value);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao criar monitor.");
                    ModelState.AddModelError(string.Empty, "Ocorreu um erro ao criar o monitor. Verifique se o PartNumber já existe.");
                }
            }
            ViewData["ColaboradorNome"] = new SelectList(GetColaboradores(), "Nome", "Nome", monitor.ColaboradorNome);
            return View(monitor);
        }

        // GET: Monitores/Edit/5
        [Authorize(Roles = "Admin,Coordenador")]
        public IActionResult Edit(string id)
        {
            if (id == null) return NotFound();
            Monitor monitor = FindMonitorById(id);
            if (monitor == null) return NotFound();
            ViewData["ColaboradorNome"] = new SelectList(GetColaboradores(), "Nome", "Nome", monitor.ColaboradorNome);
            return View(monitor);
        }

        // POST: Monitores/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Coordenador")]
        public IActionResult Edit(string id, Monitor monitor)
        {
            if (id != monitor.PartNumber) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();
                        string sql = "UPDATE Monitores SET ColaboradorNome = @ColaboradorNome, Marca = @Marca, Modelo = @Modelo, Tamanho = @Tamanho WHERE PartNumber = @PartNumber";
                        using (SqlCommand cmd = new SqlCommand(sql, connection))
                        {
                            cmd.Parameters.AddWithValue("@PartNumber", monitor.PartNumber);
                            cmd.Parameters.AddWithValue("@ColaboradorNome", (object)monitor.ColaboradorNome ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Marca", (object)monitor.Marca ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Modelo", (object)monitor.Modelo ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Tamanho", (object)monitor.Tamanho ?? DBNull.Value);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao editar monitor.");
                    ModelState.AddModelError(string.Empty, "Ocorreu um erro ao editar o monitor.");
                }
            }
            ViewData["ColaboradorNome"] = new SelectList(GetColaboradores(), "Nome", "Nome", monitor.ColaboradorNome);
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

        private List<Colaborador> GetColaboradores()
        {
            var colaboradores = new List<Colaborador>();
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string sql = "SELECT CPF, Nome FROM Colaboradores ORDER BY Nome";
                using (SqlCommand cmd = new SqlCommand(sql, connection))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            colaboradores.Add(new Colaborador {
                                CPF = reader["CPF"].ToString(),
                                Nome = reader["Nome"].ToString()
                            });
                        }
                    }
                }
            }
            return colaboradores;
        }
    }
}
