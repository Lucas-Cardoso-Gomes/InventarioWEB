using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.Sqlite;
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
using System.Linq;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin,Coordenador,Colaborador,Diretoria/RH")]
    public class ChamadosController : Controller
    {
        private readonly IDatabaseService _databaseService;
        private readonly ILogger<ChamadosController> _logger;
        private readonly IEmailService _emailService;
        private readonly IHubContext<NotificationHub> _notificationHubContext;
        private readonly IHubContext<ChatHub> _chatHubContext;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _hostingEnvironment;

        public ChamadosController(IDatabaseService databaseService, IConfiguration configuration, ILogger<ChamadosController> logger, IEmailService emailService, IHubContext<NotificationHub> notificationHubContext, IHubContext<ChatHub> chatHubContext, IWebHostEnvironment hostingEnvironment)
        {
            _databaseService = databaseService;
            _configuration = configuration;
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
                using (var connection = _databaseService.CreateConnection())
                {
                    connection.Open();
                    var sqlBuilder = new System.Text.StringBuilder(@"SELECT c.*,
                                          a.Nome as AdminNome,
                                          co.Nome as ColaboradorNome,
                                          co.Filial
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
                        parameters.Add("@StartDate", startDate.Value.ToString("yyyy-MM-dd HH:mm:ss"));
                    }

                    if (endDate.HasValue)
                    {
                        whereClauses.Add("c.DataCriacao < @EndDate");
                        parameters.Add("@EndDate", endDate.Value.Date.AddDays(1).ToString("yyyy-MM-dd HH:mm:ss"));
                    }

                    if (whereClauses.Any())
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
                                    Filial = reader["Filial"].ToString(),
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
                parameters.Add("@StartDate", startDate.Value.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            if (endDate.HasValue)
            {
                whereClauses.Add("c.DataCriacao <= @EndDate");
                parameters.Add("@EndDate", endDate.Value.AddDays(1).AddTicks(-1).ToString("yyyy-MM-dd HH:mm:ss"));
            }
            if (year.HasValue)
            {
                whereClauses.Add("strftime('%Y', c.DataCriacao) = @Year");
                parameters.Add("@Year", year.Value.ToString());
            }
            if (month.HasValue)
            {
                whereClauses.Add("strftime('%m', c.DataCriacao) = @Month");
                parameters.Add("@Month", month.Value.ToString("D2"));
            }
            if (day.HasValue)
            {
                whereClauses.Add("strftime('%d', c.DataCriacao) = @Day");
                parameters.Add("@Day", day.Value.ToString("D2"));
            }

            string whereSql = whereClauses.Any() ? "WHERE " + string.Join(" AND ", whereClauses) : "";

            using (var connection = _databaseService.CreateConnection())
            {
                connection.Open();

                // Cards
                viewModel.TotalChamados = await GetTotalChamadosAsync(connection, whereSql, parameters);
                viewModel.ChamadosAbertos = await GetCountByStatusAsync(connection, "Aberto", whereSql, parameters);
                viewModel.ChamadosEmAndamento = await GetCountByStatusAsync(connection, "Em Andamento", whereSql, parameters);
                viewModel.ChamadosFechados = await GetCountByStatusAsync(connection, "Fechado", whereSql, parameters);

                // Charts
                viewModel.Top10Servicos = await GetTop10ServicosAsync(connection, whereSql, parameters);
                _logger.LogInformation("Dashboard - Top 10 Serviços: {Count} registros encontrados.", viewModel.Top10Servicos.Count);
                viewModel.PrioridadeServicos = await GetPrioridadeServicosAsync(connection, whereSql, parameters);
                _logger.LogInformation("Dashboard - Prioridade Serviços: {Count} registros encontrados.", viewModel.PrioridadeServicos.Count);
                viewModel.Top10Usuarios = await GetTop10UsuariosAsync(connection, whereSql, parameters);
                _logger.LogInformation("Dashboard - Top 10 Usuários: {Count} registros encontrados.", viewModel.Top10Usuarios.Count);
                viewModel.HorarioMedioAbertura = await GetHorarioMedioAberturaAsync(connection, whereSql, parameters);
                _logger.LogInformation("Dashboard - Horário Médio Abertura: {Count} registros encontrados.", viewModel.HorarioMedioAbertura.Count);
                viewModel.ChamadosPorDiaDaSemana = await GetChamadosPorDiaDaSemanaAsync(connection, whereSql, parameters);
                _logger.LogInformation("Dashboard - Chamados por Dia da Semana: {Count} registros encontrados.", viewModel.ChamadosPorDiaDaSemana.Count);
                viewModel.ChamadosPorFilial = await GetChamadosPorFilialAsync(connection, whereSql, parameters);
                _logger.LogInformation("Dashboard - Chamados por Filial: {Count} registros encontrados.", viewModel.ChamadosPorFilial.Count);
                viewModel.ChamadosPorMes = await GetChamadosPorMesAsync(connection, whereSql, parameters);
                _logger.LogInformation("Dashboard - Chamados por Mês: {Count} registros encontrados.", viewModel.ChamadosPorMes.Count);
            }

            return View(viewModel);
        }

        private async Task<int> GetTotalChamadosAsync(IDbConnection connection, string whereSql, Dictionary<string, object> parameters)
        {
            var sql = $"SELECT COUNT(*) FROM Chamados c " + whereSql;
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = sql;
                foreach (var p in parameters)
                {
                    var param = cmd.CreateParameter();
                    param.ParameterName = p.Key;
                    param.Value = p.Value;
                    cmd.Parameters.Add(param);
                }
                var result = cmd.ExecuteScalar();
                return result != DBNull.Value ? Convert.ToInt32(result) : 0;
            }
        }

        private async Task<int> GetCountByStatusAsync(IDbConnection connection, string status, string whereSql, Dictionary<string, object> parameters)
        {
            var sql = $"SELECT COUNT(*) FROM Chamados c WHERE c.Status = @Status " + whereSql.Replace("WHERE", "AND");
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = sql;
                var paramStatus = cmd.CreateParameter();
                paramStatus.ParameterName = "@Status";
                paramStatus.Value = status;
                cmd.Parameters.Add(paramStatus);

                foreach (var p in parameters)
                {
                    var param = cmd.CreateParameter();
                    param.ParameterName = p.Key;
                    param.Value = p.Value;
                    cmd.Parameters.Add(param);
                }
                var result = cmd.ExecuteScalar();
                return result != DBNull.Value ? Convert.ToInt32(result) : 0;
            }
        }

        private async Task<List<ChartData>> GetTop10ServicosAsync(IDbConnection connection, string whereSql, Dictionary<string, object> parameters)
        {
            var data = new List<ChartData>();
            var sql = $"SELECT c.Servico, COUNT(*) as Count FROM Chamados c {whereSql} GROUP BY c.Servico ORDER BY Count DESC LIMIT 10";
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = sql;
                foreach (var p in parameters)
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
                        data.Add(new ChartData { Label = reader["Servico"].ToString(), Value = Convert.ToInt32(reader["Count"]) });
                    }
                }
            }
            return data;
        }

        private async Task<List<ChartData>> GetChamadosPorMesAsync(IDbConnection connection, string whereSql, Dictionary<string, object> parameters)
        {
            var data = new List<ChartData>();
            // SQLite specific date formatting
            var sql = $@"SELECT strftime('%Y-%m', c.DataCriacao) as Mes, COUNT(*) as Count
                           FROM Chamados c
                           {whereSql}
                           GROUP BY strftime('%Y-%m', c.DataCriacao)
                           ORDER BY Mes";
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = sql;
                foreach (var p in parameters)
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
                        data.Add(new ChartData { Label = reader["Mes"].ToString(), Value = Convert.ToInt32(reader["Count"]) });
                    }
                }
            }
            return data;
        }

        private async Task<List<ChartData>> GetChamadosPorDiaDaSemanaAsync(IDbConnection connection, string whereSql, Dictionary<string, object> parameters)
        {
            var data = new List<ChartData>();
            // SQLite strftime '%w' returns 0-6 where 0 is Sunday
            string sql = $@"SELECT case strftime('%w', c.DataCriacao)
                                when '0' then 'Domingo'
                                when '1' then 'Segunda'
                                when '2' then 'Terça'
                                when '3' then 'Quarta'
                                when '4' then 'Quinta'
                                when '5' then 'Sexta'
                                when '6' then 'Sábado'
                           end as DiaDaSemana, COUNT(*) as Count
                           FROM Chamados c
                           {whereSql}
                           GROUP BY strftime('%w', c.DataCriacao)
                           ORDER BY strftime('%w', c.DataCriacao)";
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = sql;
                foreach (var p in parameters)
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
                        data.Add(new ChartData { Label = reader["DiaDaSemana"].ToString(), Value = Convert.ToInt32(reader["Count"]) });
                    }
                }
            }
            return data;
        }

        private async Task<List<ChartData>> GetChamadosPorFilialAsync(IDbConnection connection, string whereSql, Dictionary<string, object> parameters)
        {
            var data = new List<ChartData>();
            string sql = $@"SELECT co.Filial, COUNT(c.ID) as Count
                           FROM Chamados c
                           JOIN Colaboradores co ON c.ColaboradorCPF = co.CPF
                           {whereSql}
                           GROUP BY co.Filial
                           ORDER BY Count DESC";
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = sql;
                foreach (var p in parameters)
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
                        data.Add(new ChartData { Label = reader["Filial"].ToString(), Value = Convert.ToInt32(reader["Count"]) });
                    }
                }
            }
            return data;
        }

        private async Task<List<ChartData>> GetPrioridadeServicosAsync(IDbConnection connection, string whereSql, Dictionary<string, object> parameters)
        {
            var data = new List<ChartData>();
            var sql = $"SELECT c.Prioridade, COUNT(*) as Count FROM Chamados c {whereSql} GROUP BY c.Prioridade";
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = sql;
                foreach (var p in parameters)
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
                        data.Add(new ChartData { Label = reader["Prioridade"].ToString(), Value = Convert.ToInt32(reader["Count"]) });
                    }
                }
            }
            return data;
        }

        private async Task<List<ChartData>> GetTop10UsuariosAsync(IDbConnection connection, string whereSql, Dictionary<string, object> parameters)
        {
            var data = new List<ChartData>();
            string sql = $@"SELECT co.Nome, COUNT(c.ID) as Count
                           FROM Chamados c
                           JOIN Colaboradores co ON c.ColaboradorCPF = co.CPF
                           {whereSql}
                           GROUP BY co.Nome
                           ORDER BY Count DESC
                           LIMIT 10";
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = sql;
                foreach (var p in parameters)
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
                        data.Add(new ChartData { Label = reader["Nome"].ToString(), Value = Convert.ToInt32(reader["Count"]) });
                    }
                }
            }
            return data;
        }

        private async Task<List<ChartData>> GetHorarioMedioAberturaAsync(IDbConnection connection, string whereSql, Dictionary<string, object> parameters)
        {
            var data = new List<ChartData>();
            string sql = $@"SELECT strftime('%H', c.DataCriacao) || ':00' as Hour, COUNT(*) as Count
                           FROM Chamados c
                           {whereSql}
                           GROUP BY strftime('%H', c.DataCriacao)
                           ORDER BY strftime('%H', c.DataCriacao)";
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = sql;
                foreach (var p in parameters)
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
                        data.Add(new ChartData { Label = reader["Hour"].ToString(), Value = Convert.ToInt32(reader["Count"]) });
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
                    using (var connection = _databaseService.CreateConnection())
                    {
                        connection.Open();
                        // SQLite: use select last_insert_rowid() instead of OUTPUT
                        string sql = @"INSERT INTO Chamados (AdminCPF, ColaboradorCPF, Servico, Descricao, DataCriacao, Status, Prioridade)
                                       VALUES (@AdminCPF, @ColaboradorCPF, @Servico, @Descricao, @DataCriacao, @Status, @Prioridade);
                                       SELECT last_insert_rowid();";
                        using (var cmd = connection.CreateCommand())
                        {
                            cmd.CommandText = sql;
                            var p1 = cmd.CreateParameter(); p1.ParameterName = "@AdminCPF"; p1.Value = adminCpfValue; cmd.Parameters.Add(p1);
                            var p2 = cmd.CreateParameter(); p2.ParameterName = "@ColaboradorCPF"; p2.Value = chamado.ColaboradorCPF; cmd.Parameters.Add(p2);
                            var p3 = cmd.CreateParameter(); p3.ParameterName = "@Servico"; p3.Value = chamado.Servico; cmd.Parameters.Add(p3);
                            var p4 = cmd.CreateParameter(); p4.ParameterName = "@Descricao"; p4.Value = chamado.Descricao; cmd.Parameters.Add(p4);
                            var p5 = cmd.CreateParameter(); p5.ParameterName = "@DataCriacao"; p5.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); cmd.Parameters.Add(p5);
                            var p6 = cmd.CreateParameter(); p6.ParameterName = "@Status"; p6.Value = chamado.Status; cmd.Parameters.Add(p6);
                            var p7 = cmd.CreateParameter(); p7.ParameterName = "@Prioridade"; p7.Value = chamado.Prioridade; cmd.Parameters.Add(p7);

                            var result = cmd.ExecuteScalar();
                            chamado.ID = Convert.ToInt32(result);
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
                using (var connection = _databaseService.CreateConnection())
                {
                    connection.Open();
                    string sql = @"SELECT DISTINCT a.CPF, a.Nome
                                   FROM Colaboradores a
                                   INNER JOIN Chamados c ON a.CPF = c.AdminCPF
                                   ORDER BY a.Nome";
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = sql;
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
                    using (var connection = _databaseService.CreateConnection())
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

                        using (var cmd = connection.CreateCommand())
                        {
                            cmd.CommandText = sql;
                            var p1 = cmd.CreateParameter(); p1.ParameterName = "@AdminCPF"; p1.Value = User.FindFirstValue("ColaboradorCPF"); cmd.Parameters.Add(p1);
                            var p2 = cmd.CreateParameter(); p2.ParameterName = "@ColaboradorCPF"; p2.Value = chamado.ColaboradorCPF; cmd.Parameters.Add(p2);
                            var p3 = cmd.CreateParameter(); p3.ParameterName = "@Servico"; p3.Value = chamado.Servico; cmd.Parameters.Add(p3);
                            var p4 = cmd.CreateParameter(); p4.ParameterName = "@Descricao"; p4.Value = chamado.Descricao; cmd.Parameters.Add(p4);
                            var p5 = cmd.CreateParameter(); p5.ParameterName = "@DataAlteracao"; p5.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); cmd.Parameters.Add(p5);
                            var p6 = cmd.CreateParameter(); p6.ParameterName = "@Status"; p6.Value = chamado.Status; cmd.Parameters.Add(p6);
                            var p7 = cmd.CreateParameter(); p7.ParameterName = "@Prioridade"; p7.Value = chamado.Prioridade; cmd.Parameters.Add(p7);
                            var p8 = cmd.CreateParameter(); p8.ParameterName = "@ID"; p8.Value = id; cmd.Parameters.Add(p8);

                            cmd.ExecuteNonQuery();
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
                using (var connection = _databaseService.CreateConnection())
                {
                    connection.Open();
                    string sql = @"SELECT c.*,
                                          a.Nome as AdminNome,
                                          co.Nome as ColaboradorNome,
                                          co.Filial
                                   FROM Chamados c
                                   LEFT JOIN Colaboradores a ON c.AdminCPF = a.CPF
                                   INNER JOIN Colaboradores co ON c.ColaboradorCPF = co.CPF
                                   WHERE c.ID = @ID";
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = sql;
                        var p1 = cmd.CreateParameter(); p1.ParameterName = "@ID"; p1.Value = id; cmd.Parameters.Add(p1);
                        using (var reader = cmd.ExecuteReader())
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
                                     Filial = reader["Filial"].ToString(),
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
                using (var connection = _databaseService.CreateConnection())
                {
                    connection.Open();
                    string sql = "DELETE FROM Chamados WHERE ID = @ID";
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = sql;
                        var p1 = cmd.CreateParameter(); p1.ParameterName = "@ID"; p1.Value = id; cmd.Parameters.Add(p1);
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
                using (var connection = _databaseService.CreateConnection())
                {
                    var adminCpf = User.FindFirstValue("ColaboradorCPF");
                    connection.Open();
                    string sql = "UPDATE Chamados SET Status = 'Aberto', AdminCPF = @AdminCPF, DataAlteracao = @DataAlteracao WHERE ID = @ID";
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = sql;
                        var p1 = cmd.CreateParameter(); p1.ParameterName = "@AdminCPF"; p1.Value = (object)adminCpf ?? DBNull.Value; cmd.Parameters.Add(p1);
                        var p2 = cmd.CreateParameter(); p2.ParameterName = "@DataAlteracao"; p2.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); cmd.Parameters.Add(p2);
                        var p3 = cmd.CreateParameter(); p3.ParameterName = "@ID"; p3.Value = id; cmd.Parameters.Add(p3);
                        cmd.ExecuteNonQuery();
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
                using (var connection = _databaseService.CreateConnection())
                {
                    var adminCpf = User.FindFirstValue("ColaboradorCPF");
                    connection.Open();
                    string sql = "UPDATE Chamados SET Status = 'Em Andamento', AdminCPF = @AdminCPF, DataAlteracao = @DataAlteracao WHERE ID = @ID";
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = sql;
                        var p1 = cmd.CreateParameter(); p1.ParameterName = "@AdminCPF"; p1.Value = (object)adminCpf ?? DBNull.Value; cmd.Parameters.Add(p1);
                        var p2 = cmd.CreateParameter(); p2.ParameterName = "@DataAlteracao"; p2.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); cmd.Parameters.Add(p2);
                        var p3 = cmd.CreateParameter(); p3.ParameterName = "@ID"; p3.Value = id; cmd.Parameters.Add(p3);
                        cmd.ExecuteNonQuery();
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
                using (var connection = _databaseService.CreateConnection())
                {
                    connection.Open();
                    string sql = "UPDATE Chamados SET Status = 'Fechado', DataAlteracao = @DataAlteracao WHERE ID = @ID";
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = sql;
                        var p1 = cmd.CreateParameter(); p1.ParameterName = "@DataAlteracao"; p1.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); cmd.Parameters.Add(p1);
                        var p2 = cmd.CreateParameter(); p2.ParameterName = "@ID"; p2.Value = id; cmd.Parameters.Add(p2);
                        cmd.ExecuteNonQuery();
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
                using (var connection = _databaseService.CreateConnection())
                {
                    connection.Open();
                    string sql = "SELECT DISTINCT Servico FROM Chamados ORDER BY Servico";
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = sql;
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
        using (var connection = _databaseService.CreateConnection())
        {
            connection.Open();
            string sql = "SELECT DISTINCT Servico FROM Chamados ORDER BY Servico";
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = sql;
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
                using (var connection = _databaseService.CreateConnection())
                {
                    connection.Open();
                    string sql = @"SELECT cc.*, c.Nome as UsuarioNome
                                   FROM ChamadoConversas cc
                                   JOIN Colaboradores c ON cc.UsuarioCPF = c.CPF
                                   WHERE cc.ChamadoID = @ChamadoID
                                   ORDER BY cc.DataCriacao";
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = sql;
                        var p1 = cmd.CreateParameter(); p1.ParameterName = "@ChamadoID"; p1.Value = chamadoId; cmd.Parameters.Add(p1);
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
                using (var connection = _databaseService.CreateConnection())
                {
                    connection.Open();
                    string sql = "SELECT * FROM ChamadoAnexos WHERE ChamadoID = @ChamadoID ORDER BY DataUpload";
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = sql;
                        var p1 = cmd.CreateParameter(); p1.ParameterName = "@ChamadoID"; p1.Value = chamadoId; cmd.Parameters.Add(p1);
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

                using (var connection = _databaseService.CreateConnection())
                {
                    connection.Open();
                    var sql = @"INSERT INTO ChamadoAnexos (ChamadoID, NomeArquivo, CaminhoArquivo, DataUpload)
                                VALUES (@ChamadoID, @NomeArquivo, @CaminhoArquivo, @DataUpload)";
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = sql;
                        var p1 = cmd.CreateParameter(); p1.ParameterName = "@ChamadoID"; p1.Value = ChamadoID; cmd.Parameters.Add(p1);
                        var p2 = cmd.CreateParameter(); p2.ParameterName = "@NomeArquivo"; p2.Value = file.FileName; cmd.Parameters.Add(p2);
                        var p3 = cmd.CreateParameter(); p3.ParameterName = "@CaminhoArquivo"; p3.Value = dbPath; cmd.Parameters.Add(p3);
                        var p4 = cmd.CreateParameter(); p4.ParameterName = "@DataUpload"; p4.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); cmd.Parameters.Add(p4);
                        cmd.ExecuteNonQuery();
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
                using (var connection = _databaseService.CreateConnection())
                {
                    connection.Open();
                    var sql = @"INSERT INTO ChamadoConversas (ChamadoID, UsuarioCPF, Mensagem, DataCriacao)
                                VALUES (@ChamadoID, @UsuarioCPF, @Mensagem, @DataCriacao);
                                SELECT last_insert_rowid();";
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = sql;
                        var p1 = cmd.CreateParameter(); p1.ParameterName = "@ChamadoID"; p1.Value = chamadoId; cmd.Parameters.Add(p1);
                        var p2 = cmd.CreateParameter(); p2.ParameterName = "@UsuarioCPF"; p2.Value = userCpf; cmd.Parameters.Add(p2);
                        var p3 = cmd.CreateParameter(); p3.ParameterName = "@Mensagem"; p3.Value = message; cmd.Parameters.Add(p3);
                        var p4 = cmd.CreateParameter(); p4.ParameterName = "@DataCriacao"; p4.Value = timestamp.ToString("yyyy-MM-dd HH:mm:ss"); cmd.Parameters.Add(p4);

                        var result = cmd.ExecuteScalar();
                        var newId = Convert.ToInt32(result);

                        await _chatHubContext.Clients.Group(chamadoId.ToString()).SendAsync("ReceiveMessage", userName, message, timestamp.ToString("o"));
                        return Ok(new { id = newId, timestamp = timestamp });
                    }
                }
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
                    using (var connection = _databaseService.CreateConnection())
                    {
                        connection.Open();
                        string sql = "SELECT Email, Nome FROM Colaboradores WHERE CPF = @CPF";
                        using (var cmd = connection.CreateCommand())
                        {
                            cmd.CommandText = sql;
                            var p1 = cmd.CreateParameter(); p1.ParameterName = "@CPF"; p1.Value = chamado.ColaboradorCPF; cmd.Parameters.Add(p1);
                            using (var reader = cmd.ExecuteReader())
                            {
                                if (reader.Read())
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
