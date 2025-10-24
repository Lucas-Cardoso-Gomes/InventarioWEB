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
using Web.Services;
using Microsoft.AspNetCore.SignalR;
using Web.Hubs;
using System.IO;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin,Coordenador,Colaborador,Diretoria/RH")]
    public class ChamadosController : Controller
    {
        private readonly string _connectionString;
        private readonly ILogger<ChamadosController> _logger;
        private readonly IEmailService _emailService;
        private readonly IHubContext<NotificationHub> _notificationHubContext;
        private readonly IHubContext<ChatHub> _chatHubContext;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public ChamadosController(IConfiguration configuration, ILogger<ChamadosController> logger, IEmailService emailService, IHubContext<NotificationHub> notificationHubContext, IHubContext<ChatHub> chatHubContext, IWebHostEnvironment hostingEnvironment)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
            _emailService = emailService;
            _notificationHubContext = notificationHubContext;
            _chatHubContext = chatHubContext;
            _hostingEnvironment = hostingEnvironment;
        }

        public IActionResult Index(List<string> statuses, List<string> selectedAdmins, List<string> selectedColaboradores, List<string> selectedServicos, string searchText, DateTime? startDate, DateTime? endDate)
        {
            if (statuses == null || !statuses.Any())
            {
                statuses = new List<string> { "Aberto", "Em Andamento" };
            }

            ViewBag.SelectedStatuses = statuses;
            ViewBag.Admins = GetAdminsFromChamados();
            ViewBag.SelectedAdmins = selectedAdmins;
            ViewBag.Colaboradores = GetColaboradores();
            ViewBag.SelectedColaboradores = selectedColaboradores;
            ViewBag.Servicos = GetAllServicos();
            ViewBag.SelectedServicos = selectedServicos;
            ViewBag.SearchText = searchText;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;

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

                    if (User.IsInRole("Coordenador") && !User.IsInRole("Admin") && !User.IsInRole("Diretoria/RH"))
                    {
                        whereClauses.Add("(co.CoordenadorCPF = @UserCpf OR c.ColaboradorCPF = @UserCpf)");
                        parameters.Add("@UserCpf", (object)userCpf ?? DBNull.Value);
                    }
                    else if (User.IsInRole("Colaborador") && !User.IsInRole("Admin") && !User.IsInRole("Diretoria/RH"))
                    {
                        whereClauses.Add("c.ColaboradorCPF = @UserCpf");
                        parameters.Add("@UserCpf", (object)userCpf ?? DBNull.Value);
                    }

                    if (statuses.Any())
                    {
                        var statusClauses = new List<string>();
                        for (int i = 0; i < statuses.Count; i++)
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

                    if (selectedColaboradores != null && selectedColaboradores.Any())
                    {
                        var colabClauses = new List<string>();
                        for (int i = 0; i < selectedColaboradores.Count; i++)
                        {
                            var paramName = $"@ColabCPF{i}";
                            colabClauses.Add(paramName);
                            parameters.Add(paramName, selectedColaboradores[i]);
                        }
                        whereClauses.Add($"c.ColaboradorCPF IN ({string.Join(", ", colabClauses)})");
                    }

                    if (selectedServicos != null && selectedServicos.Any())
                    {
                        var servicoClauses = new List<string>();
                        for (int i = 0; i < selectedServicos.Count; i++)
                        {
                            var paramName = $"@Servico{i}";
                            servicoClauses.Add(paramName);
                            parameters.Add(paramName, selectedServicos[i]);
                        }
                        whereClauses.Add($"c.Servico IN ({string.Join(", ", servicoClauses)})");
                    }

                    if (!string.IsNullOrWhiteSpace(searchText))
                    {
                        whereClauses.Add("(c.Descricao LIKE @SearchText OR c.Servico LIKE @SearchText)");
                        parameters.Add("@SearchText", $"%{searchText}%");
                    }

                    if (startDate.HasValue)
                    {
                        whereClauses.Add("c.DataCriacao >= @StartDate");
                        parameters.Add("@StartDate", startDate.Value.Date);
                    }

                    if (endDate.HasValue)
                    {
                        whereClauses.Add("c.DataCriacao < @EndDate");
                        parameters.Add("@EndDate", endDate.Value.Date.AddDays(1));
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
                                     Prioridade = reader["Prioridade"].ToString(),
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

        [Authorize(Roles = "Admin,Diretoria/RH")]
        public async Task<IActionResult> Dashboard(DateTime? startDate, DateTime? endDate, int? year, int? month, int? day)
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
                await connection.OpenAsync();

                // Cards
                viewModel.TotalChamados = await GetTotalChamadosAsync(connection, whereSql, parameters);
                viewModel.ChamadosAbertos = await GetCountByStatusAsync(connection, "Aberto", whereSql, parameters);
                viewModel.ChamadosEmAndamento = await GetCountByStatusAsync(connection, "Em Andamento", whereSql, parameters);
                viewModel.ChamadosFechados = await GetCountByStatusAsync(connection, "Fechado", whereSql, parameters);

                // Charts
                viewModel.Top10Servicos = await GetTop10ServicosAsync(connection, whereSql, parameters);
                viewModel.PrioridadeServicos = await GetPrioridadeServicosAsync(connection, whereSql, parameters);
                viewModel.Top10Usuarios = await GetTop10UsuariosAsync(connection, whereSql, parameters);
                viewModel.HorarioMedioAbertura = await GetHorarioMedioAberturaAsync(connection, whereSql, parameters);
                viewModel.TopDiasDaSemana = await GetTopDiasDaSemanaAsync(connection, whereSql, parameters);
            }

            return View(viewModel);
        }

        private async Task<int> GetTotalChamadosAsync(SqlConnection connection, string whereSql, Dictionary<string, object> parameters)
        {
            var sql = $"SELECT COUNT(*) FROM Chamados " + whereSql;
            using (var cmd = new SqlCommand(sql, connection))
            {
                foreach (var p in parameters)
                {
                    cmd.Parameters.AddWithValue(p.Key, p.Value);
                }
                return (int)await cmd.ExecuteScalarAsync();
            }
        }

        private async Task<int> GetCountByStatusAsync(SqlConnection connection, string status, string whereSql, Dictionary<string, object> parameters)
        {
            var sql = $"SELECT COUNT(*) FROM Chamados WHERE Status = @Status " + whereSql.Replace("WHERE", "AND");
            using (var cmd = new SqlCommand(sql, connection))
            {
                cmd.Parameters.AddWithValue("@Status", status);
                foreach (var p in parameters)
                {
                    cmd.Parameters.AddWithValue(p.Key, p.Value);
                }
                return (int)await cmd.ExecuteScalarAsync();
            }
        }

        private async Task<List<ChartData>> GetTop10ServicosAsync(SqlConnection connection, string whereSql, Dictionary<string, object> parameters)
        {
            var data = new List<ChartData>();
            var sql = $"SELECT TOP 10 Servico, COUNT(*) as Count FROM Chamados {whereSql} GROUP BY Servico ORDER BY Count DESC";
            using (var cmd = new SqlCommand(sql, connection))
            {
                foreach (var p in parameters)
                {
                    cmd.Parameters.AddWithValue(p.Key, p.Value);
                }
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        data.Add(new ChartData { Label = reader["Servico"].ToString(), Value = (int)reader["Count"] });
                    }
                }
            }
            return data;
        }

        private async Task<List<ChartData>> GetPrioridadeServicosAsync(SqlConnection connection, string whereSql, Dictionary<string, object> parameters)
        {
            var data = new List<ChartData>();
            var sql = $"SELECT Prioridade, COUNT(*) as Count FROM Chamados {whereSql} GROUP BY Prioridade";
            using (var cmd = new SqlCommand(sql, connection))
            {
                foreach (var p in parameters)
                {
                    cmd.Parameters.AddWithValue(p.Key, p.Value);
                }
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        data.Add(new ChartData { Label = reader["Prioridade"].ToString(), Value = (int)reader["Count"] });
                    }
                }
            }
            return data;
        }

        private async Task<List<ChartData>> GetTop10UsuariosAsync(SqlConnection connection, string whereSql, Dictionary<string, object> parameters)
        {
            var data = new List<ChartData>();
            string sql = $@"SELECT TOP 10 co.Nome, COUNT(c.ID) as Count
                           FROM Chamados c
                           JOIN Colaboradores co ON c.ColaboradorCPF = co.CPF
                           {whereSql}
                           GROUP BY co.Nome
                           ORDER BY Count DESC";
            using (var cmd = new SqlCommand(sql, connection))
            {
                foreach (var p in parameters)
                {
                    cmd.Parameters.AddWithValue(p.Key, p.Value);
                }
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        data.Add(new ChartData { Label = reader["Nome"].ToString(), Value = (int)reader["Count"] });
                    }
                }
            }
            return data;
        }

        private async Task<List<ChartData>> GetHorarioMedioAberturaAsync(SqlConnection connection, string whereSql, Dictionary<string, object> parameters)
        {
            var data = new List<ChartData>();
            string sql = $@"SELECT CAST(DATEPART(hour, c.DataCriacao) AS NVARCHAR(2)) + ':00' as Hour, COUNT(*) as Count
                           FROM Chamados c
                           {whereSql}
                           GROUP BY DATEPART(hour, c.DataCriacao)
                           ORDER BY DATEPART(hour, c.DataCriacao)";
            using (var cmd = new SqlCommand(sql, connection))
            {
                foreach (var p in parameters)
                {
                    cmd.Parameters.AddWithValue(p.Key, p.Value);
                }
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        data.Add(new ChartData { Label = reader["Hour"].ToString(), Value = (int)reader["Count"] });
                    }
                }
            }
            return data;
        }

        private async Task<List<ChartData>> GetTopDiasDaSemanaAsync(SqlConnection connection, string whereSql, Dictionary<string, object> parameters)
        {
            var data = new List<ChartData>();
            string sql = $@"SET DATEFIRST 1;
                           SELECT
                               CASE DATEPART(weekday, c.DataCriacao)
                                   WHEN 1 THEN 'Segunda-feira'
                                   WHEN 2 THEN 'Terça-feira'
                                   WHEN 3 THEN 'Quarta-feira'
                                   WHEN 4 THEN 'Quinta-feira'
                                   WHEN 5 THEN 'Sexta-feira'
                                   WHEN 6 THEN 'Sábado'
                                   WHEN 7 THEN 'Domingo'
                               END as DiaDaSemana,
                               COUNT(*) as Count
                           FROM Chamados c
                           {whereSql}
                           GROUP BY DATEPART(weekday, c.DataCriacao)
                           ORDER BY DATEPART(weekday, c.DataCriacao)";
            using (var cmd = new SqlCommand(sql, connection))
            {
                foreach (var p in parameters)
                {
                    cmd.Parameters.AddWithValue(p.Key, p.Value);
                }
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        data.Add(new ChartData { Label = reader["DiaDaSemana"].ToString(), Value = (int)reader["Count"] });
                    }
                }
            }
            return data;
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
        public async Task<IActionResult> Create(Chamado chamado)
        {
            var userCpf = User.FindFirstValue("ColaboradorCPF");
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            object adminCpfValue = DBNull.Value;

            if (!User.IsInRole("Admin"))
            {
                chamado.ColaboradorCPF = userCpf;
                ModelState.Remove(nameof(Chamado.ColaboradorCPF));
            }
            else
            {
                adminCpfValue = userCpf;
            }

            if (ModelState.IsValid)
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();
                        string sql = @"INSERT INTO Chamados (AdminCPF, ColaboradorCPF, Servico, Descricao, DataCriacao, Status, Prioridade)
                                       OUTPUT INSERTED.ID
                                       VALUES (@AdminCPF, @ColaboradorCPF, @Servico, @Descricao, @DataCriacao, @Status, @Prioridade)";
                        using (SqlCommand cmd = new SqlCommand(sql, connection))
                        {
                            cmd.Parameters.AddWithValue("@AdminCPF", adminCpfValue);
                            cmd.Parameters.AddWithValue("@ColaboradorCPF", chamado.ColaboradorCPF);
                            cmd.Parameters.AddWithValue("@Servico", chamado.Servico);
                            cmd.Parameters.AddWithValue("@Descricao", chamado.Descricao);
                            cmd.Parameters.AddWithValue("@DataCriacao", DateTime.Now);
                            cmd.Parameters.AddWithValue("@Status", chamado.Status);
                            cmd.Parameters.AddWithValue("@Prioridade", chamado.Prioridade);
                            chamado.ID = (int)await cmd.ExecuteScalarAsync();
                        }
                    }

                    _ = Task.Run(() => SendNotificationAsync(chamado, "Criado", userId));

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

        // GET: Chamados/Chat/5
        public IActionResult Chat(int id)
        {
            var conversas = GetConversasByChamadoId(id);
            ViewBag.ChamadoID = id;
            return PartialView("_Chat", conversas);
        }

        // GET: Chamados/Details/5
        public IActionResult Details(int? id)
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

            chamado.Conversas = GetConversasByChamadoId(id.Value);
            chamado.Anexos = GetAnexosByChamadoId(id.Value);

            ViewBag.ChamadoID = id.Value;

            return View(chamado);
        }

        // GET: Chamados/Edit/5
        [Authorize(Roles = "Admin,Coordenador,Colaborador,Diretoria/RH")]
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
        [Authorize(Roles = "Admin,Coordenador,Colaborador,Diretoria/RH")]
        public async Task<IActionResult> Edit(int id, Chamado chamado)
        {
            if (id != chamado.ID)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

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
                                       Status = @Status,
                                       Prioridade = @Prioridade
                                       WHERE ID = @ID";

                        using (SqlCommand cmd = new SqlCommand(sql, connection))
                        {
                            cmd.Parameters.AddWithValue("@AdminCPF", User.FindFirstValue("ColaboradorCPF"));
                            cmd.Parameters.AddWithValue("@ColaboradorCPF", chamado.ColaboradorCPF);
                            cmd.Parameters.AddWithValue("@Servico", chamado.Servico);
                            cmd.Parameters.AddWithValue("@Descricao", chamado.Descricao);
                            cmd.Parameters.AddWithValue("@DataAlteracao", DateTime.Now);
                            cmd.Parameters.AddWithValue("@Status", chamado.Status);
                            cmd.Parameters.AddWithValue("@Prioridade", chamado.Prioridade);
                            cmd.Parameters.AddWithValue("@ID", id);
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }

                    _ = Task.Run(() => SendNotificationAsync(chamado, "Editado", userId));
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
                                    Prioridade = reader["Prioridade"].ToString(),
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
        [Authorize(Roles = "Admin,Coordenador,Colaborador,Diretoria/RH")]
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
        [Authorize(Roles = "Admin,Coordenador,Colaborador,Diretoria/RH")]
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
        public async Task<IActionResult> ReopenTicket(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    var adminCpf = User.FindFirstValue("ColaboradorCPF");
                    connection.Open();
                    string sql = "UPDATE Chamados SET Status = 'Aberto', AdminCPF = @AdminCPF, DataAlteracao = @DataAlteracao WHERE ID = @ID";
                    using (SqlCommand cmd = new SqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@AdminCPF", (object)adminCpf ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@DataAlteracao", DateTime.Now);
                        cmd.Parameters.AddWithValue("@ID", id);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                var chamado = FindChamadoById(id);
                if (chamado != null)
                {
                    _ = Task.Run(() => SendNotificationAsync(chamado, "Reaberto", userId));
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
        public async Task<IActionResult> WorkingTicket(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    var adminCpf = User.FindFirstValue("ColaboradorCPF");
                    connection.Open();
                    string sql = "UPDATE Chamados SET Status = 'Em Andamento', AdminCPF = @AdminCPF, DataAlteracao = @DataAlteracao WHERE ID = @ID";
                    using (SqlCommand cmd = new SqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@AdminCPF", (object)adminCpf ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@DataAlteracao", DateTime.Now);
                        cmd.Parameters.AddWithValue("@ID", id);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                var chamado = FindChamadoById(id);
                if (chamado != null)
                {
                    _ = Task.Run(() => SendNotificationAsync(chamado, "Em Andamento", userId));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar chamado.");
                TempData["ErrorMessage"] = "Erro ao atualizar o chamado.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CloseTicket(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
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
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                var chamado = FindChamadoById(id);
                if (chamado != null)
                {
                    _ = Task.Run(() => SendNotificationAsync(chamado, "Fechado", userId));
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

        // GET: Chamados/GetServicos
        [HttpGet]
        public IActionResult GetServicos()
        {
            var servicos = new List<string>();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    string sql = "SELECT DISTINCT Servico FROM Chamados ORDER BY Servico";
                    using (var cmd = new SqlCommand(sql, connection))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                servicos.Add(reader["Servico"].ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter a lista de serviços.");
                return StatusCode(500, "Erro interno do servidor");
            }
            return Json(servicos);
        }

        private List<string> GetAllServicos()
{
    var servicos = new List<string>();
    try
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            connection.Open();
            string sql = "SELECT DISTINCT Servico FROM Chamados ORDER BY Servico";
            using (var cmd = new SqlCommand(sql, connection))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        servicos.Add(reader["Servico"].ToString());
                    }
                }
            }
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Erro ao obter a lista de serviços.");
    }
    return servicos;
}

        private List<ChamadoConversa> GetConversasByChamadoId(int chamadoId)
        {
            var conversas = new List<ChamadoConversa>();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    string sql = @"SELECT cc.*, c.Nome as UsuarioNome
                                   FROM ChamadoConversas cc
                                   JOIN Colaboradores c ON cc.UsuarioCPF = c.CPF
                                   WHERE cc.ChamadoID = @ChamadoID
                                   ORDER BY cc.DataCriacao";
                    using (var cmd = new SqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@ChamadoID", chamadoId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                conversas.Add(new ChamadoConversa
                                {
                                    ID = Convert.ToInt32(reader["ID"]),
                                    ChamadoID = Convert.ToInt32(reader["ChamadoID"]),
                                    UsuarioCPF = reader["UsuarioCPF"].ToString(),
                                    Mensagem = reader["Mensagem"].ToString(),
                                    DataCriacao = Convert.ToDateTime(reader["DataCriacao"]),
                                    UsuarioNome = reader["UsuarioNome"].ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter as conversas do chamado.");
            }
            return conversas;
        }

        private List<ChamadoAnexo> GetAnexosByChamadoId(int chamadoId)
        {
            var anexos = new List<ChamadoAnexo>();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    string sql = "SELECT * FROM ChamadoAnexos WHERE ChamadoID = @ChamadoID ORDER BY DataUpload";
                    using (var cmd = new SqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@ChamadoID", chamadoId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                anexos.Add(new ChamadoAnexo
                                {
                                    ID = Convert.ToInt32(reader["ID"]),
                                    ChamadoID = Convert.ToInt32(reader["ChamadoID"]),
                                    NomeArquivo = reader["NomeArquivo"].ToString(),
                                    CaminhoArquivo = reader["CaminhoArquivo"].ToString(),
                                    DataUpload = Convert.ToDateTime(reader["DataUpload"])
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter os anexos do chamado.");
            }
            return anexos;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAnexo(int ChamadoID, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return Json(new { success = false, message = "Nenhum arquivo selecionado." });
            }

            var uploadsFolderPath = Path.Combine(_hostingEnvironment.WebRootPath, "attachments", ChamadoID.ToString());
            if (!Directory.Exists(uploadsFolderPath))
            {
                Directory.CreateDirectory(uploadsFolderPath);
            }

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
            var filePath = Path.Combine(uploadsFolderPath, uniqueFileName);
            var dbPath = $"/attachments/{ChamadoID}/{uniqueFileName}";

            try
            {
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"INSERT INTO ChamadoAnexos (ChamadoID, NomeArquivo, CaminhoArquivo, DataUpload)
                                VALUES (@ChamadoID, @NomeArquivo, @CaminhoArquivo, @DataUpload)";
                    using (var cmd = new SqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@ChamadoID", ChamadoID);
                        cmd.Parameters.AddWithValue("@NomeArquivo", file.FileName);
                        cmd.Parameters.AddWithValue("@CaminhoArquivo", dbPath);
                        cmd.Parameters.AddWithValue("@DataUpload", DateTime.Now);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                await _chatHubContext.Clients.Group(ChamadoID.ToString()).SendAsync("NewAttachment", file.FileName, Url.Content(dbPath));
                return Json(new { success = true, fileName = file.FileName, filePath = Url.Content(dbPath) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao fazer upload do anexo.");
                return Json(new { success = false, message = "Erro ao fazer upload do anexo." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendChatMessage(int chamadoId, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return BadRequest("A mensagem não pode estar vazia.");
            }

            var userCpf = User.FindFirstValue("ColaboradorCPF");
            var userName = User.Identity.Name;
            var timestamp = DateTime.Now;

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"INSERT INTO ChamadoConversas (ChamadoID, UsuarioCPF, Mensagem, DataCriacao)
                                OUTPUT INSERTED.ID, INSERTED.DataCriacao
                                VALUES (@ChamadoID, @UsuarioCPF, @Mensagem, @DataCriacao)";
                    using (var cmd = new SqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@ChamadoID", chamadoId);
                        cmd.Parameters.AddWithValue("@UsuarioCPF", userCpf);
                        cmd.Parameters.AddWithValue("@Mensagem", message);
                        cmd.Parameters.AddWithValue("@DataCriacao", timestamp);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var newId = reader.GetInt32(0);
                                var newTimestamp = reader.GetDateTime(1);

                                await _chatHubContext.Clients.Group(chamadoId.ToString()).SendAsync("ReceiveMessage", userName, message, newTimestamp.ToString("o"));
                                return Ok(new { id = newId, timestamp = newTimestamp });
                            }
                        }
                    }
                }
                return StatusCode(500, "Falha ao salvar a mensagem.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar mensagem do chat.");
                return StatusCode(500, "Erro interno do servidor.");
            }
        }

        private async Task SendNotificationAsync(Chamado chamado, string status, string userId)
        {
            _logger.LogInformation("Iniciando o processo de notificação para o chamado ID {ChamadoID} com status {Status}", chamado.ID, status);

            try
            {
                string colaboradorEmail = null;
                try
                {
                    using (var connection = new SqlConnection(_connectionString))
                    {
                        await connection.OpenAsync();
                        string sql = "SELECT Email, Nome FROM Colaboradores WHERE CPF = @CPF";
                        using (var cmd = new SqlCommand(sql, connection))
                        {
                            cmd.Parameters.AddWithValue("@CPF", chamado.ColaboradorCPF);
                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    colaboradorEmail = reader["Email"]?.ToString();
                                    if (string.IsNullOrEmpty(chamado.ColaboradorNome))
                                    {
                                        chamado.ColaboradorNome = reader["Nome"]?.ToString();
                                    }
                                    _logger.LogInformation("E-mail do colaborador encontrado: {Email}", colaboradorEmail);
                                }
                                else
                                {
                                    _logger.LogWarning("Nenhum colaborador encontrado para o CPF {CPF}", chamado.ColaboradorCPF);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao buscar e-mail do colaborador para o CPF {CPF}", chamado.ColaboradorCPF);
                }

                if (string.IsNullOrEmpty(chamado.ColaboradorNome))
                {
                    _logger.LogWarning("Não foi possível encontrar o nome do colaborador para o CPF {ColaboradorCPF}, usando o CPF como fallback.", chamado.ColaboradorCPF);
                    chamado.ColaboradorNome = chamado.ColaboradorCPF;
                }

                var subject = $"PM Logística: Chamado {status} \"{chamado.Servico}\" por \"{chamado.ColaboradorNome}\" às \"{DateTime.Now:dd/MM/yyyy HH:mm}\"";
                var messageBuilder = new System.Text.StringBuilder();
                messageBuilder.AppendLine($"Um chamado foi {status.ToLower()}: <br/>");
                messageBuilder.AppendLine($"<b>Serviço:</b> {chamado.Servico}<br/>");
                messageBuilder.AppendLine($"<b>Colaborador:</b> {chamado.ColaboradorNome}<br/>");
                messageBuilder.AppendLine($"<b>Descrição:</b> {chamado.Descricao}<br/>");
                messageBuilder.AppendLine($"<b>Prioridade:</b> {chamado.Prioridade}<br/>");
                messageBuilder.AppendLine($"<b>Status:</b> {status}<br/>");

                if (status == "Fechado")
                {
                    _logger.LogInformation("Anexando histórico de chat e lista de anexos para o chamado fechado ID {ChamadoID}", chamado.ID);
                    var conversas = GetConversasByChamadoId(chamado.ID);
                    if (conversas.Any())
                    {
                        messageBuilder.AppendLine("<br/><b>Histórico do Chat:</b><br/>");
                        messageBuilder.AppendLine("<ul>");
                        foreach (var conversa in conversas)
                        {
                            messageBuilder.AppendLine($"<li><b>{conversa.UsuarioNome}</b> ({conversa.DataCriacao:dd/MM/yyyy HH:mm}): {conversa.Mensagem}</li>");
                        }
                        messageBuilder.AppendLine("</ul>");
                    }

                    var anexos = GetAnexosByChamadoId(chamado.ID);
                    if (anexos.Any())
                    {
                        messageBuilder.AppendLine("<br/><b>Arquivos Anexados:</b><br/>");
                        messageBuilder.AppendLine("<ul>");
                        foreach (var anexo in anexos)
                        {
                            messageBuilder.AppendLine($"<li>{anexo.NomeArquivo}</li>");
                        }
                        messageBuilder.AppendLine("</ul>");
                    }
                }

                var message = messageBuilder.ToString();
                var toEmailGroup = _configuration.GetValue<string>("EmailSettings:ToEmail");
                var recipients = new List<string>();
                if (!string.IsNullOrEmpty(toEmailGroup))
                {
                    recipients.AddRange(toEmailGroup.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries));
                }
                if (!string.IsNullOrEmpty(colaboradorEmail))
                {
                    recipients.Add(colaboradorEmail);
                }

                if (recipients.Any())
                {
                    foreach (var email in recipients.Distinct())
                    {
                        try
                        {
                            await _emailService.SendEmailAsync(email, subject, message);
                            _logger.LogInformation("E-mail de notificação enviado com sucesso para {Email}", email);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Falha ao enviar e-mail de notificação para {Email}", email);
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("Nenhum destinatário de e-mail encontrado. O e-mail de notificação não será enviado.");
                }

                await _notificationHubContext.Clients.All.SendAsync("ReceiveNotification", "Atualização de Chamado", subject);
                _logger.LogInformation("Notificação via SignalR enviada para todos os clientes.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha geral ao enviar notificação para o chamado ID {ChamadoID}", chamado.ID);
                if (!string.IsNullOrEmpty(userId))
                {
                    try
                    {
                        await _notificationHubContext.Clients.User(userId).SendAsync("ReceiveError", "Falha ao enviar notificação", "Ocorreu um erro ao tentar enviar a notificação. O chamado foi salvo, mas a notificação falhou.");
                        _logger.LogInformation("Notificação de erro via SignalR enviada para o usuário {UserID}", userId);
                    }
                    catch (Exception signalREx)
                    {
                        _logger.LogError(signalREx, "Falha ao enviar a notificação de erro via SignalR para o usuário {UserID}", userId);
                    }
                }
            }
        }
    }
}
