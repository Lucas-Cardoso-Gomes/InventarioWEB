using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Web.Models;
using Web.Services;
using System.Security.Claims;
using System.Data;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin,Coordenador,Colaborador,Diretoria")]
    public class PerifericosController : Controller
    {
        private readonly IDatabaseService _databaseService;
        private readonly ILogger<PerifericosController> _logger;

        public PerifericosController(IDatabaseService databaseService, ILogger<PerifericosController> logger, PersistentLogService persistentLogService)
        {
            _databaseService = databaseService;
            _logger = logger;
        }

        // GET: Perifericos
        public IActionResult Index(string searchString)
        {
            ViewData["CurrentFilter"] = searchString;
            var perifericos = new List<Periferico>();
            try
            {
                using (var connection = _databaseService.CreateConnection())
                {
                    connection.Open();

                    var sqlBuilder = new System.Text.StringBuilder("SELECT p.*, c.Nome as ColaboradorNome FROM Perifericos p LEFT JOIN Colaboradores c ON p.ColaboradorCPF = c.CPF");
                    var whereClauses = new List<string>();
                    var parameters = new Dictionary<string, object>();
                    var userCpf = User.FindFirstValue("ColaboradorCPF");

                    if (User.IsInRole("Colaborador") && !User.IsInRole("Admin") && !User.IsInRole("Diretoria"))
                    {
                        whereClauses.Add("p.ColaboradorCPF = @UserCpf");
                        parameters.Add("@UserCpf", (object)userCpf ?? DBNull.Value);
                    }
                    else if (User.IsInRole("Coordenador") && !User.IsInRole("Admin") && !User.IsInRole("Diretoria"))
                    {
                        whereClauses.Add("(c.CoordenadorCPF = @UserCpf OR p.ColaboradorCPF = @UserCpf)");
                        parameters.Add("@UserCpf", (object)userCpf ?? DBNull.Value);
                    }

                    if (!string.IsNullOrEmpty(searchString))
                    {
                        whereClauses.Add("(c.Nome LIKE @search OR p.Tipo LIKE @search OR p.PartNumber LIKE @search)");
                        parameters.Add("@search", $"%{searchString}%");
                    }

                    if (whereClauses.Count > 0)
                    {
                        sqlBuilder.Append(" WHERE " + string.Join(" AND ", whereClauses));
                    }

                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = sqlBuilder.ToString();
                        foreach(var p in parameters)
                        {
                            var param = cmd.CreateParameter();
                            param.ParameterName = p.Key;
                            param.Value = p.Value;
                            cmd.Parameters.Add(param);
                        }
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                perifericos.Add(new Periferico
                                {
                                    PartNumber = reader["PartNumber"].ToString(),
                                    ColaboradorCPF = reader["ColaboradorCPF"] as string,
                                    ColaboradorNome = reader["ColaboradorNome"] as string,
                                    Tipo = reader["Tipo"].ToString(),
                                    DataEntrega = reader["DataEntrega"] != DBNull.Value ? Convert.ToDateTime(reader["DataEntrega"]) : (DateTime?)null
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
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewData["Colaboradores"] = new SelectList(GetColaboradores(), "CPF", "Nome");
            return View();
        }

        // POST: Perifericos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult Create(Periferico periferico)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (var connection = _databaseService.CreateConnection())
                    {
                        connection.Open();
                        string sql = "INSERT INTO Perifericos (PartNumber, ColaboradorCPF, Tipo, DataEntrega) VALUES (@PartNumber, @ColaboradorCPF, @Tipo, @DataEntrega)";
                        using (var cmd = connection.CreateCommand())
                        {
                            cmd.CommandText = sql;
                            var p1 = cmd.CreateParameter(); p1.ParameterName = "@PartNumber"; p1.Value = periferico.PartNumber; cmd.Parameters.Add(p1);
                            var p2 = cmd.CreateParameter(); p2.ParameterName = "@ColaboradorCPF"; p2.Value = (object)periferico.ColaboradorCPF ?? DBNull.Value; cmd.Parameters.Add(p2);
                            var p3 = cmd.CreateParameter(); p3.ParameterName = "@Tipo"; p3.Value = periferico.Tipo; cmd.Parameters.Add(p3);
                            var p4 = cmd.CreateParameter(); p4.ParameterName = "@DataEntrega"; p4.Value = (object)periferico.DataEntrega ?? DBNull.Value; cmd.Parameters.Add(p4);
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
            ViewData["Colaboradores"] = new SelectList(GetColaboradores(), "CPF", "Nome", periferico.ColaboradorCPF);
            return View(periferico);
        }

        // GET: Perifericos/Edit/5
        [Authorize(Roles = "Admin")]
        public IActionResult Edit(string id)
        {
            Periferico periferico = FindPerifericoById(id);
            if (periferico == null) return NotFound();
            ViewData["Colaboradores"] = new SelectList(GetColaboradores(), "CPF", "Nome", periferico.ColaboradorCPF);
            return View(periferico);
        }

        // POST: Perifericos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult Edit(string id, Periferico periferico)
        {
            if (id != periferico.PartNumber) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    using (var connection = _databaseService.CreateConnection())
                    {
                        connection.Open();
                        string sql = "UPDATE Perifericos SET ColaboradorCPF = @ColaboradorCPF, Tipo = @Tipo, DataEntrega = @DataEntrega WHERE PartNumber = @PartNumber";
                        using (var cmd = connection.CreateCommand())
                        {
                            cmd.CommandText = sql;
                            var p1 = cmd.CreateParameter(); p1.ParameterName = "@PartNumber"; p1.Value = periferico.PartNumber; cmd.Parameters.Add(p1);
                            var p2 = cmd.CreateParameter(); p2.ParameterName = "@ColaboradorCPF"; p2.Value = (object)periferico.ColaboradorCPF ?? DBNull.Value; cmd.Parameters.Add(p2);
                            var p3 = cmd.CreateParameter(); p3.ParameterName = "@Tipo"; p3.Value = periferico.Tipo; cmd.Parameters.Add(p3);
                            var p4 = cmd.CreateParameter(); p4.ParameterName = "@DataEntrega"; p4.Value = (object)periferico.DataEntrega ?? DBNull.Value; cmd.Parameters.Add(p4);
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
            ViewData["Colaboradores"] = new SelectList(GetColaboradores(), "CPF", "Nome", periferico.ColaboradorCPF);
            return View(periferico);
        }

        // GET: Perifericos/Delete/5
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(string id)
        {
            Periferico periferico = FindPerifericoById(id);
            if (periferico == null) return NotFound();
            return View(periferico);
        }

        // POST: Perifericos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult DeleteConfirmed(string id)
        {
            try
            {
                var periferico = FindPerifericoById(id);
                if (periferico != null)
                {
                    using (var connection = _databaseService.CreateConnection())
                    {
                        connection.Open();
                        string sql = "DELETE FROM Perifericos WHERE PartNumber = @PartNumber";
                        using (var cmd = connection.CreateCommand())
                        {
                            cmd.CommandText = sql;
                            var p1 = cmd.CreateParameter(); p1.ParameterName = "@PartNumber"; p1.Value = id; cmd.Parameters.Add(p1);
                            cmd.ExecuteNonQuery();
                        }
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

        private Periferico FindPerifericoById(string id)
        {
            Periferico periferico = null;
            using (var connection = _databaseService.CreateConnection())
            {
                connection.Open();
                string sql = "SELECT p.*, c.Nome AS ColaboradorNome FROM Perifericos p LEFT JOIN Colaboradores c ON p.ColaboradorCPF = c.CPF WHERE p.PartNumber = @PartNumber";
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = sql;
                    var p1 = cmd.CreateParameter(); p1.ParameterName = "@PartNumber"; p1.Value = id; cmd.Parameters.Add(p1);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            periferico = new Periferico
                            {
                                PartNumber = reader["PartNumber"].ToString(),
                                ColaboradorCPF = reader["ColaboradorCPF"] as string,
                                ColaboradorNome = reader["ColaboradorNome"] as string,
                                Tipo = reader["Tipo"].ToString(),
                                DataEntrega = reader["DataEntrega"] != DBNull.Value ? Convert.ToDateTime(reader["DataEntrega"]) : (DateTime?)null
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
            using (var connection = _databaseService.CreateConnection())
            {
                connection.Open();
                string sql = "SELECT CPF, Nome FROM Colaboradores ORDER BY Nome";
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = sql;
                    using (var reader = cmd.ExecuteReader())
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
