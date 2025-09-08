using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Web.Models;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin,Coordenador,Colaborador")]
    public class ChamadosController : Controller
    {
        private readonly string _connectionString;
        private readonly ILogger<ChamadosController> _logger;

        public ChamadosController(IConfiguration configuration, ILogger<ChamadosController> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
        }

        public IActionResult Index()
        {
            var chamados = new List<Chamado>();
            var userCpf = User.FindFirstValue("ColaboradorCPF");

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    string sql = @"SELECT c.*,
                                          a.Nome as AdminNome,
                                          co.Nome as ColaboradorNome
                                   FROM Chamados c
                                   LEFT JOIN Colaboradores a ON c.AdminCPF = a.CPF
                                   INNER JOIN Colaboradores co ON c.ColaboradorCPF = co.CPF";

                    if (User.IsInRole("Colaborador"))
                    {
                        sql += " WHERE c.ColaboradorCPF = @UserCpf";
                    }
                    else if (User.IsInRole("Coordenador"))
                    {
                        sql += " WHERE co.CoordenadorCPF = @UserCpf";
                    }

                    using (SqlCommand cmd = new SqlCommand(sql, connection))
                    {
                        if (!User.IsInRole("Admin"))
                        {
                            cmd.Parameters.AddWithValue("@UserCpf", (object)userCpf ?? DBNull.Value);
                        }

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                chamados.Add(new Chamado
                                {
                                    ID = Convert.ToInt32(reader["ID"]),
                                    AdminCPF = reader["AdminCPF"] != DBNull.Value ? reader["AdminCPF"].ToString() : null,
                                    ColaboradorCPF = reader["ColaboradorCPF"].ToString(),
                                    Descricao = reader["Descricao"].ToString(),
                                    DataAlteracao = reader["DataAlteracao"] != DBNull.Value ? Convert.ToDateTime(reader["DataAlteracao"]) : (DateTime?)null,
                                    DataCriacao = Convert.ToDateTime(reader["DataCriacao"]),
                                    AdminNome = reader["AdminNome"] != DBNull.Value ? reader["AdminNome"].ToString() : null,
                                    ColaboradorNome = reader["ColaboradorNome"].ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter a lista de chamados.");
            }

            return View(chamados);
        }

        // GET: Chamados/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewBag.Colaboradores = new SelectList(GetColaboradores(), "CPF", "Nome");
            return View();
        }

        // POST: Chamados/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult Create(Chamado chamado)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();
                        string sql = @"INSERT INTO Chamados (AdminCPF, ColaboradorCPF, Descricao, DataCriacao)
                                       VALUES (@AdminCPF, @ColaboradorCPF, @Descricao, @DataCriacao)";
                        using (SqlCommand cmd = new SqlCommand(sql, connection))
                        {
                            cmd.Parameters.AddWithValue("@AdminCPF", User.FindFirstValue("ColaboradorCPF"));
                            cmd.Parameters.AddWithValue("@ColaboradorCPF", chamado.ColaboradorCPF);
                            cmd.Parameters.AddWithValue("@Descricao", chamado.Descricao);
                            cmd.Parameters.AddWithValue("@DataCriacao", DateTime.Now);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao criar chamado.");
                    ModelState.AddModelError(string.Empty, "Ocorreu um erro ao criar o chamado.");
                }
            }
            ViewBag.Colaboradores = new SelectList(GetColaboradores(), "CPF", "Nome", chamado.ColaboradorCPF);
            return View(chamado);
        }

        private List<Colaborador> GetColaboradores()
        {
            var colaboradores = new List<Colaborador>();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    string sql = "SELECT CPF, Nome FROM Colaboradores ORDER BY Nome";
                    using (var cmd = new SqlCommand(sql, connection))
                    {
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
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter a lista de colaboradores.");
            }
            return colaboradores;
        }

        // GET: Chamados/Edit/5
        [Authorize(Roles = "Admin")]
        public IActionResult Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var chamado = FindChamadoById(id.Value);
            if (chamado == null)
            {
                return NotFound();
            }
            ViewBag.Colaboradores = new SelectList(GetColaboradores(), "CPF", "Nome", chamado.ColaboradorCPF);
            return View(chamado);
        }

        // POST: Chamados/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult Edit(int id, Chamado chamado)
        {
            if (id != chamado.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();
                        string sql = @"UPDATE Chamados SET
                                       AdminCPF = @AdminCPF,
                                       ColaboradorCPF = @ColaboradorCPF,
                                       Descricao = @Descricao,
                                       DataAlteracao = @DataAlteracao
                                       WHERE ID = @ID";
                        using (SqlCommand cmd = new SqlCommand(sql, connection))
                        {
                            cmd.Parameters.AddWithValue("@AdminCPF", User.FindFirstValue("ColaboradorCPF"));
                            cmd.Parameters.AddWithValue("@ColaboradorCPF", chamado.ColaboradorCPF);
                            cmd.Parameters.AddWithValue("@Descricao", chamado.Descricao);
                            cmd.Parameters.AddWithValue("@DataAlteracao", DateTime.Now);
                            cmd.Parameters.AddWithValue("@ID", id);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao editar chamado.");
                    ModelState.AddModelError(string.Empty, "Ocorreu um erro ao editar o chamado.");
                    ViewBag.Colaboradores = new SelectList(GetColaboradores(), "CPF", "Nome", chamado.ColaboradorCPF);
                    return View(chamado);
                }
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Colaboradores = new SelectList(GetColaboradores(), "CPF", "Nome", chamado.ColaboradorCPF);
            return View(chamado);
        }

        private Chamado FindChamadoById(int id)
        {
            Chamado chamado = null;
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    string sql = @"SELECT c.*,
                                          a.Nome as AdminNome,
                                          co.Nome as ColaboradorNome
                                   FROM Chamados c
                                   LEFT JOIN Colaboradores a ON c.AdminCPF = a.CPF
                                   INNER JOIN Colaboradores co ON c.ColaboradorCPF = co.CPF
                                   WHERE c.ID = @ID";
                    using (SqlCommand cmd = new SqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@ID", id);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                chamado = new Chamado
                                {
                                    ID = Convert.ToInt32(reader["ID"]),
                                    AdminCPF = reader["AdminCPF"] != DBNull.Value ? reader["AdminCPF"].ToString() : null,
                                    ColaboradorCPF = reader["ColaboradorCPF"].ToString(),
                                    Descricao = reader["Descricao"].ToString(),
                                    DataAlteracao = reader["DataAlteracao"] != DBNull.Value ? Convert.ToDateTime(reader["DataAlteracao"]) : (DateTime?)null,
                                    DataCriacao = Convert.ToDateTime(reader["DataCriacao"]),
                                    AdminNome = reader["AdminNome"] != DBNull.Value ? reader["AdminNome"].ToString() : null,
                                    ColaboradorNome = reader["ColaboradorNome"].ToString()
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao encontrar chamado por ID.");
            }
            return chamado;
        }

        // GET: Chamados/Delete/5
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var chamado = FindChamadoById(id.Value);
            if (chamado == null)
            {
                return NotFound();
            }

            return View(chamado);
        }

        // POST: Chamados/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult DeleteConfirmed(int id)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    string sql = "DELETE FROM Chamados WHERE ID = @ID";
                    using (SqlCommand cmd = new SqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@ID", id);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao excluir chamado.");
                ViewBag.ErrorMessage = "Erro ao excluir o chamado.";
                var chamado = FindChamadoById(id);
                return View(chamado);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
