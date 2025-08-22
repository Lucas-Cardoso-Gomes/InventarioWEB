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
    public class PerifericosController : Controller
    {
        private readonly string _connectionString;
        private readonly ILogger<PerifericosController> _logger;

        public PerifericosController(IConfiguration configuration, ILogger<PerifericosController> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
        }

        // GET: Perifericos
        public IActionResult Index(string searchString)
        {
            ViewData["CurrentFilter"] = searchString;
            var perifericos = new List<Periferico>();
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    string sql = "SELECT * FROM Perifericos";
                    if (!string.IsNullOrEmpty(searchString))
                    {
                        sql += " WHERE ColaboradorNome LIKE @search OR Tipo LIKE @search OR PartNumber LIKE @search";
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
                                perifericos.Add(new Periferico
                                {
                                    ID = Convert.ToInt32(reader["ID"]),
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
        public IActionResult Create()
        {
            ViewData["ColaboradorNome"] = new SelectList(GetColaboradores(), "Nome", "Nome");
            return View();
        }

        // POST: Perifericos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Coordenador")]
        public IActionResult Create(Periferico periferico)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();
                        string sql = "INSERT INTO Perifericos (ColaboradorNome, Tipo, DataEntrega, PartNumber) VALUES (@ColaboradorNome, @Tipo, @DataEntrega, @PartNumber)";
                        using (SqlCommand cmd = new SqlCommand(sql, connection))
                        {
                            cmd.Parameters.AddWithValue("@ColaboradorNome", (object)periferico.ColaboradorNome ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Tipo", periferico.Tipo);
                            cmd.Parameters.AddWithValue("@DataEntrega", (object)periferico.DataEntrega ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@PartNumber", (object)periferico.PartNumber ?? DBNull.Value);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao criar periférico.");
                    ModelState.AddModelError(string.Empty, "Ocorreu um erro ao criar o periférico.");
                }
            }
            ViewData["ColaboradorNome"] = new SelectList(GetColaboradores(), "Nome", "Nome", periferico.ColaboradorNome);
            return View(periferico);
        }

        // GET: Perifericos/Edit/5
        [Authorize(Roles = "Admin,Coordenador")]
        public IActionResult Edit(int id)
        {
            Periferico periferico = FindPerifericoById(id);
            if (periferico == null) return NotFound();
            ViewData["ColaboradorNome"] = new SelectList(GetColaboradores(), "Nome", "Nome", periferico.ColaboradorNome);
            return View(periferico);
        }

        // POST: Perifericos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Coordenador")]
        public IActionResult Edit(int id, Periferico periferico)
        {
            if (id != periferico.ID) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();
                        string sql = "UPDATE Perifericos SET ColaboradorNome = @ColaboradorNome, Tipo = @Tipo, DataEntrega = @DataEntrega, PartNumber = @PartNumber WHERE ID = @ID";
                        using (SqlCommand cmd = new SqlCommand(sql, connection))
                        {
                            cmd.Parameters.AddWithValue("@ID", periferico.ID);
                            cmd.Parameters.AddWithValue("@ColaboradorNome", (object)periferico.ColaboradorNome ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Tipo", periferico.Tipo);
                            cmd.Parameters.AddWithValue("@DataEntrega", (object)periferico.DataEntrega ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@PartNumber", (object)periferico.PartNumber ?? DBNull.Value);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao editar periférico.");
                    ModelState.AddModelError(string.Empty, "Ocorreu um erro ao editar o periférico.");
                }
            }
            ViewData["ColaboradorNome"] = new SelectList(GetColaboradores(), "Nome", "Nome", periferico.ColaboradorNome);
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
                string sql = "SELECT * FROM Perifericos WHERE ID = @ID";
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
