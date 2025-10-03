using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Web.Models;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin,Coordenador,Colaborador,Diretoria")]
    public class ChamadosController : Controller
    {
        private readonly string _connectionString;
        private readonly ILogger<ChamadosController> _logger;

        public ChamadosController(IConfiguration configuration, ILogger<ChamadosController> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
        }

        public IActionResult Index(List<string> statuses, List<string> selectedAdmins)
        {
            if (statuses == null || !statuses.Any())
            {
                statuses = new List<string> { "Aberto", "Em Andamento" };
            }
            ViewBag.SelectedStatuses = statuses;
            ViewBag.Admins = GetAdminsFromChamados();
            ViewBag.SelectedAdmins = selectedAdmins;


            var chamados = new List<Chamado>();
            var userCpf = User.FindFirstValue("ColaboradorCPF");

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var sqlBuilder = new System.Text.StringBuilder(@"SELECT c.*,
                                          a.Nome as AdminNome,
                                          co.Nome as ColaboradorNome
                                   FROM Chamados c
                                   LEFT JOIN Colaboradores a ON c.AdminCPF = a.CPF
                                   INNER JOIN Colaboradores co ON c.ColaboradorCPF = co.CPF");

                    var whereClauses = new List<string>();
                    var parameters = new Dictionary<string, object>();

                    if (User.IsInRole("Coordenador") && !User.IsInRole("Admin") && !User.IsInRole("Diretoria"))
                    {
                        whereClauses.Add("(co.CoordenadorCPF = @UserCpf OR c.ColaboradorCPF = @UserCpf)");
                        parameters.Add("@UserCpf", (object)userCpf ?? DBNull.Value);
                    }
                    else if (User.IsInRole("Colaborador") && !User.IsInRole("Admin") && !User.IsInRole("Diretoria"))
                    {
                        whereClauses.Add("c.ColaboradorCPF = @UserCpf");
                        parameters.Add("@UserCpf", (object)userCpf ?? DBNull.Value);
                    }

                    if (statuses.Any())
                    {
                        var statusClauses = new List<string>();
                        for(int i = 0; i < statuses.Count; i++)
                        {
                            var paramName = $"@Status{i}";
                            statusClauses.Add(paramName);
                            parameters.Add(paramName, statuses[i]);
                        }
                        whereClauses.Add($"c.Status IN ({string.Join(", ", statusClauses)})");
                    }

                    if (selectedAdmins != null && selectedAdmins.Any())
                    {
                        var adminClauses = new List<string>();
                        for (int i = 0; i < selectedAdmins.Count; i++)
                        {
                            var paramName = $"@AdminCPF{i}";
                            adminClauses.Add(paramName);
                            parameters.Add(paramName, selectedAdmins[i]);
                        }
                        whereClauses.Add($"c.AdminCPF IN ({string.Join(", ", adminClauses)})");
                    }

                    if (whereClauses.Any())
                    {
                        sqlBuilder.Append(" WHERE " + string.Join(" AND ", whereClauses));
                    }

                    using (SqlCommand cmd = new SqlCommand(sqlBuilder.ToString(), connection))
                    {
                        foreach(var p in parameters)
                        {
                            cmd.Parameters.AddWithValue(p.Key, p.Value);
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
                                    Servico = reader["Servico"].ToString(),
                                    Descricao = reader["Descricao"].ToString(),
                                    DataAlteracao = reader["DataAlteracao"] != DBNull.Value ? Convert.ToDateTime(reader["DataAlteracao"]) : (DateTime?)null,
                                    DataCriacao = Convert.ToDateTime(reader["DataCriacao"]),
                                    Status = reader["Status"].ToString(),
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

        [Authorize(Roles = "Admin,Coordenador")]
        public IActionResult Dashboard(DateTime? startDate, DateTime? endDate, int? year, int? month, int? day)
        {
            var viewModel = new ChamadoDashboardViewModel();
            var whereClauses = new List<string>();
            var parameters = new Dictionary<string, object>();

            if (startDate.HasValue)
            {
                whereClauses.Add("c.DataCriacao >= @StartDate");
                parameters.Add("@StartDate", startDate.Value);
            }
            if (endDate.HasValue)
            {
                whereClauses.Add("c.DataCriacao <= @EndDate");
                parameters.Add("@EndDate", endDate.Value.AddDays(1).AddTicks(-1));
            }
            if (year.HasValue)
            {
                whereClauses.Add("YEAR(c.DataCriacao) = @Year");
                parameters.Add("@Year", year.Value);
            }
            if (month.HasValue)
            {
                whereClauses.Add("MONTH(c.DataCriacao) = @Month");
                parameters.Add("@Month", month.Value);
            }
            if (day.HasValue)
            {
                whereClauses.Add("DAY(c.DataCriacao) = @Day");
                parameters.Add("@Day", day.Value);
            }

            string whereSql = whereClauses.Any() ? "WHERE " + string.Join(" AND ", whereClauses) : "";

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                // Total de Chamados
                string totalSql = $"SELECT COUNT(*) FROM Chamados c {whereSql}";
                using (var cmd = new SqlCommand(totalSql, connection))
                {
                    foreach (var p in parameters) cmd.Parameters.AddWithValue(p.Key, p.Value);
                    viewModel.TotalChamados = (int)cmd.ExecuteScalar();
                }

                // Top 10 Serviços
                string topServicosSql = $@"SELECT TOP 10 Servico, COUNT(*) as Count
                                           FROM Chamados c {whereSql}
                                           GROUP BY Servico
                                           ORDER BY Count DESC";
                using (var cmd = new SqlCommand(topServicosSql, connection))
                {
                    foreach (var p in parameters) cmd.Parameters.AddWithValue(p.Key, p.Value);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            viewModel.Top10Servicos.Add(new ChartData { Label = reader["Servico"].ToString(), Value = (int)reader["Count"] });
                        }
                    }
                }

                // Total de Chamados por Admin
                string porAdminSql = $@"SELECT a.Nome, COUNT(c.ID) as Count
                                        FROM Chamados c
                                        JOIN Colaboradores a ON c.AdminCPF = a.CPF
                                        {whereSql}
                                        GROUP BY a.Nome
                                        ORDER BY Count DESC";
                using (var cmd = new SqlCommand(porAdminSql, connection))
                {
                    foreach (var p in parameters) cmd.Parameters.AddWithValue(p.Key, p.Value);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            viewModel.TotalChamadosPorAdmin.Add(new ChartData { Label = reader["Nome"].ToString(), Value = (int)reader["Count"] });
                        }
                    }
                }

                // Top 10 Colaboradores
                string topColaboradoresSql = $@"SELECT TOP 10 co.Nome, COUNT(c.ID) as Count
                                                FROM Chamados c
                                                JOIN Colaboradores co ON c.ColaboradorCPF = co.CPF
                                                {whereSql}
                                                GROUP BY co.Nome
                                                ORDER BY Count DESC";
                using (var cmd = new SqlCommand(topColaboradoresSql, connection))
                {
                    foreach (var p in parameters) cmd.Parameters.AddWithValue(p.Key, p.Value);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            viewModel.Top10Colaboradores.Add(new ChartData { Label = reader["Nome"].ToString(), Value = (int)reader["Count"] });
                        }
                    }
                }
            }

            return View(viewModel);
        }

        // GET: Chamados/Create
        public IActionResult Create()
        {
            if (User.IsInRole("Admin"))
            {
                ViewBag.Colaboradores = new SelectList(GetColaboradores(), "CPF", "Nome");
            }
            return View();
        }

        // POST: Chamados/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Chamado chamado)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();
                        string sql = @"INSERT INTO Chamados (AdminCPF, ColaboradorCPF, Servico, Descricao, DataCriacao, Status)
                                       VALUES (@AdminCPF, @ColaboradorCPF, @Servico, @Descricao, @DataCriacao, @Status)";
                        using (SqlCommand cmd = new SqlCommand(sql, connection))
                        {
                            var userCpf = User.FindFirstValue("ColaboradorCPF");
                            if (User.IsInRole("Admin"))
                            {
                                cmd.Parameters.AddWithValue("@AdminCPF", userCpf);
                                cmd.Parameters.AddWithValue("@ColaboradorCPF", chamado.ColaboradorCPF);
                            }
                            else
                            {
                                cmd.Parameters.AddWithValue("@AdminCPF", DBNull.Value);
                                cmd.Parameters.AddWithValue("@ColaboradorCPF", userCpf);
                            }

                            cmd.Parameters.AddWithValue("@Servico", chamado.Servico);
                            cmd.Parameters.AddWithValue("@Descricao", chamado.Descricao);
                            cmd.Parameters.AddWithValue("@DataCriacao", DateTime.Now);
                            cmd.Parameters.AddWithValue("@Status", chamado.Status);
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
            if (User.IsInRole("Admin"))
            {
                ViewBag.Colaboradores = new SelectList(GetColaboradores(), "CPF", "Nome", chamado.ColaboradorCPF);
            }
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

        private List<Colaborador> GetAdminsFromChamados()
        {
            var admins = new List<Colaborador>();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    string sql = @"SELECT DISTINCT a.CPF, a.Nome
                                   FROM Colaboradores a
                                   INNER JOIN Chamados c ON a.CPF = c.AdminCPF
                                   ORDER BY a.Nome";
                    using (var cmd = new SqlCommand(sql, connection))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                admins.Add(new Colaborador
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
                _logger.LogError(ex, "Erro ao obter a lista de admins de chamados.");
            }
            return admins;
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
                                       Servico = @Servico,
                                       Descricao = @Descricao,
                                       DataAlteracao = @DataAlteracao,
                                       Status = @Status
                                       WHERE ID = @ID";

                        using (SqlCommand cmd = new SqlCommand(sql, connection))
                        {
                            cmd.Parameters.AddWithValue("@AdminCPF", User.FindFirstValue("ColaboradorCPF"));
                            cmd.Parameters.AddWithValue("@ColaboradorCPF", chamado.ColaboradorCPF);
                            cmd.Parameters.AddWithValue("@Servico", chamado.Servico);
                            cmd.Parameters.AddWithValue("@Descricao", chamado.Descricao);
                            cmd.Parameters.AddWithValue("@DataAlteracao", DateTime.Now);
                            cmd.Parameters.AddWithValue("@Status", chamado.Status);
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
                                    Servico = reader["Servico"].ToString(),
                                    Descricao = reader["Descricao"].ToString(),
                                    DataAlteracao = reader["DataAlteracao"] != DBNull.Value ? Convert.ToDateTime(reader["DataAlteracao"]) : (DateTime?)null,
                                    DataCriacao = Convert.ToDateTime(reader["DataCriacao"]),
                                    Status = reader["Status"].ToString(),
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

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public IActionResult ReopenTicket(int id)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    string sql = "UPDATE Chamados SET Status = 'Aberto', DataAlteracao = @DataAlteracao WHERE ID = @ID";
                    using (SqlCommand cmd = new SqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@DataAlteracao", DateTime.Now);
                        cmd.Parameters.AddWithValue("@ID", id);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao reabrir chamado.");
                TempData["ErrorMessage"] = "Erro ao reabrir o chamado.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public IActionResult CloseTicket(int id)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    string sql = "UPDATE Chamados SET Status = 'Fechado', DataAlteracao = @DataAlteracao WHERE ID = @ID";
                    using (SqlCommand cmd = new SqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@DataAlteracao", DateTime.Now);
                        cmd.Parameters.AddWithValue("@ID", id);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao fechar chamado.");
                // Optionally, add a message to the user that something went wrong
                TempData["ErrorMessage"] = "Erro ao fechar o chamado.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
