using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using web.Models;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin,Coordenador")]
    public class ColaboradoresController : Controller
    {
        private readonly string _connectionString;
        private readonly ILogger<ColaboradoresController> _logger;

        public ColaboradoresController(IConfiguration configuration, ILogger<ColaboradoresController> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
        }

        // GET: Colaboradores
        public IActionResult Index(string searchString)
        {
            ViewData["CurrentFilter"] = searchString;
            var colaboradores = new List<Colaborador>();
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    string sql = "SELECT * FROM Colaboradores";
                    if (!string.IsNullOrEmpty(searchString))
                    {
                        sql += " WHERE Nome LIKE @search OR CPF LIKE @search OR Email LIKE @search";
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
                                colaboradores.Add(new Colaborador
                                {
                                    CPF = reader["CPF"].ToString(),
                                    Nome = reader["Nome"].ToString(),
                                    Email = reader["Email"].ToString(),
                                    SenhaEmail = reader["SenhaEmail"].ToString(),
                                    Teams = reader["Teams"].ToString(),
                                    SenhaTeams = reader["SenhaTeams"].ToString(),
                                    EDespacho = reader["EDespacho"].ToString(),
                                    SenhaEDespacho = reader["SenhaEDespacho"].ToString(),
                                    Genius = reader["Genius"].ToString(),
                                    SenhaGenius = reader["SenhaGenius"].ToString(),
                                    Ibrooker = reader["Ibrooker"].ToString(),
                                    SenhaIbrooker = reader["SenhaIbrooker"].ToString(),
                                    Adicional = reader["Adicional"].ToString(),
                                    SenhaAdicional = reader["SenhaAdicional"].ToString(),
                                    Setor = reader["Setor"].ToString(),
                                    Ramal = reader["Ramal"].ToString(),
                                    Alarme = reader["Alarme"].ToString(),
                                    Videoporteiro = reader["Videoporteiro"].ToString(),
                                    Obs = reader["Obs"].ToString(),
                                    DataInclusao = reader["DataInclusao"] != DBNull.Value ? Convert.ToDateTime(reader["DataInclusao"]) : (DateTime?)null,
                                    DataAlteracao = reader["DataAlteracao"] != DBNull.Value ? Convert.ToDateTime(reader["DataAlteracao"]) : (DateTime?)null
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter a lista de colaboradores.");
                // Handle error, maybe return an error view
            }
            return View(colaboradores);
        }

        // GET: Colaboradores/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Colaboradores/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult Create(Colaborador colaborador)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();
                        string sql = @"INSERT INTO Colaboradores (CPF, Nome, Email, SenhaEmail, Teams, SenhaTeams, EDespacho, SenhaEDespacho, Genius, SenhaGenius, Ibrooker, SenhaIbrooker, Adicional, SenhaAdicional, Setor, Ramal, Alarme, Videoporteiro, Obs, DataInclusao)
                                       VALUES (@CPF, @Nome, @Email, @SenhaEmail, @Teams, @SenhaTeams, @EDespacho, @SenhaEDespacho, @Genius, @SenhaGenius, @Ibrooker, @SenhaIbrooker, @Adicional, @SenhaAdicional, @Setor, @Ramal, @Alarme, @Videoporteiro, @Obs, @DataInclusao)";
                        using (SqlCommand cmd = new SqlCommand(sql, connection))
                        {
                            cmd.Parameters.AddWithValue("@CPF", colaborador.CPF);
                            cmd.Parameters.AddWithValue("@Nome", colaborador.Nome);
                            cmd.Parameters.AddWithValue("@Email", (object)colaborador.Email ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@SenhaEmail", (object)colaborador.SenhaEmail ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Teams", (object)colaborador.Teams ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@SenhaTeams", (object)colaborador.SenhaTeams ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@EDespacho", (object)colaborador.EDespacho ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@SenhaEDespacho", (object)colaborador.SenhaEDespacho ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Genius", (object)colaborador.Genius ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@SenhaGenius", (object)colaborador.SenhaGenius ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Ibrooker", (object)colaborador.Ibrooker ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@SenhaIbrooker", (object)colaborador.SenhaIbrooker ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Adicional", (object)colaborador.Adicional ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@SenhaAdicional", (object)colaborador.SenhaAdicional ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Setor", (object)colaborador.Setor ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Ramal", (object)colaborador.Ramal ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Alarme", (object)colaborador.Alarme ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Videoporteiro", (object)colaborador.Videoporteiro ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Obs", (object)colaborador.Obs ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@DataInclusao", DateTime.Now);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao criar colaborador.");
                    ModelState.AddModelError(string.Empty, "Ocorreu um erro ao criar o colaborador. Verifique se o CPF já existe.");
                }
            }
            return View(colaborador);
        }

        // GET: Colaboradores/Edit/5
        [Authorize(Roles = "Admin")]
        public IActionResult Edit(string id)
        {
            if (id == null) return NotFound();
            Colaborador colaborador = FindColaboradorById(id);
            if (colaborador == null) return NotFound();
            return View(colaborador);
        }

        // POST: Colaboradores/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult Edit(string id, Colaborador colaborador)
        {
            if (id != colaborador.CPF) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();
                        string sql = @"UPDATE Colaboradores SET
                                       Nome = @Nome, Email = @Email, SenhaEmail = @SenhaEmail, Teams = @Teams, SenhaTeams = @SenhaTeams,
                                       EDespacho = @EDespacho, SenhaEDespacho = @SenhaEDespacho, Genius = @Genius, SenhaGenius = @SenhaGenius,
                                       Ibrooker = @Ibrooker, SenhaIbrooker = @SenhaIbrooker, Adicional = @Adicional, SenhaAdicional = @SenhaAdicional,
                                       Setor = @Setor, Ramal = @Ramal, Alarme = @Alarme, Videoporteiro = @Videoporteiro,
                                       Obs = @Obs, DataAlteracao = @DataAlteracao
                                       WHERE CPF = @CPF";
                        using (SqlCommand cmd = new SqlCommand(sql, connection))
                        {
                            cmd.Parameters.AddWithValue("@CPF", colaborador.CPF);
                            cmd.Parameters.AddWithValue("@Nome", colaborador.Nome);
                            cmd.Parameters.AddWithValue("@Email", (object)colaborador.Email ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@SenhaEmail", (object)colaborador.SenhaEmail ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Teams", (object)colaborador.Teams ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@SenhaTeams", (object)colaborador.SenhaTeams ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@EDespacho", (object)colaborador.EDespacho ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@SenhaEDespacho", (object)colaborador.SenhaEDespacho ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Genius", (object)colaborador.Genius ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@SenhaGenius", (object)colaborador.SenhaGenius ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Ibrooker", (object)colaborador.Ibrooker ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@SenhaIbrooker", (object)colaborador.SenhaIbrooker ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Adicional", (object)colaborador.Adicional ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@SenhaAdicional", (object)colaborador.SenhaAdicional ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Setor", (object)colaborador.Setor ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Ramal", (object)colaborador.Ramal ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Alarme", (object)colaborador.Alarme ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Videoporteiro", (object)colaborador.Videoporteiro ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Obs", (object)colaborador.Obs ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@DataAlteracao", DateTime.Now);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao editar colaborador.");
                    ModelState.AddModelError(string.Empty, "Ocorreu um erro ao editar o colaborador.");
                }
            }
            return View(colaborador);
        }

        // GET: Colaboradores/Delete/5
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(string id)
        {
            if (id == null) return NotFound();
            Colaborador colaborador = FindColaboradorById(id);
            if (colaborador == null) return NotFound();
            return View(colaborador);
        }

        // POST: Colaboradores/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult DeleteConfirmed(string id)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    // You might want to check for foreign key dependencies before deleting
                    string sql = "DELETE FROM Colaboradores WHERE CPF = @CPF";
                    using (SqlCommand cmd = new SqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@CPF", id);
                        cmd.ExecuteNonQuery();
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao excluir colaborador.");
                // Redirect to a view with the error message
                Colaborador colaborador = FindColaboradorById(id);
                ViewBag.ErrorMessage = "Erro ao excluir. Verifique se o colaborador está associado a computadores, monitores ou periféricos.";
                return View(colaborador);
            }
        }

        private Colaborador FindColaboradorById(string id)
        {
            Colaborador colaborador = null;
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    string sql = "SELECT * FROM Colaboradores WHERE CPF = @CPF";
                    using (SqlCommand cmd = new SqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@CPF", id);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                colaborador = new Colaborador
                                {
                                    CPF = reader["CPF"].ToString(),
                                    Nome = reader["Nome"].ToString(),
                                    Email = reader["Email"].ToString(),
                                    SenhaEmail = reader["SenhaEmail"].ToString(),
                                    Teams = reader["Teams"].ToString(),
                                    SenhaTeams = reader["SenhaTeams"].ToString(),
                                    EDespacho = reader["EDespacho"].ToString(),
                                    SenhaEDespacho = reader["SenhaEDespacho"].ToString(),
                                    Genius = reader["Genius"].ToString(),
                                    SenhaGenius = reader["SenhaGenius"].ToString(),
                                    Ibrooker = reader["Ibrooker"].ToString(),
                                    SenhaIbrooker = reader["SenhaIbrooker"].ToString(),
                                    Adicional = reader["Adicional"].ToString(),
                                    SenhaAdicional = reader["SenhaAdicional"].ToString(),
                                    Setor = reader["Setor"].ToString(),
                                    Ramal = reader["Ramal"].ToString(),
                                    Alarme = reader["Alarme"].ToString(),
                                    Videoporteiro = reader["Videoporteiro"].ToString(),
                                    Obs = reader["Obs"].ToString(),
                                    DataInclusao = reader["DataInclusao"] != DBNull.Value ? Convert.ToDateTime(reader["DataInclusao"]) : (DateTime?)null,
                                    DataAlteracao = reader["DataAlteracao"] != DBNull.Value ? Convert.ToDateTime(reader["DataAlteracao"]) : (DateTime?)null
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao encontrar colaborador por ID.");
            }
            return colaborador;
        }
    }
}
