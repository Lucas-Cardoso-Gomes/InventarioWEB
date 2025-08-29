using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using web.Models;
using System.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Web.Services;
using System.Threading.Tasks;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin,Coordenador,Normal")]
    public class ComputadoresController : Controller
    {
        private readonly string _connectionString;
        private readonly ILogger<ComputadoresController> _logger;
        private readonly PersistentLogService _persistentLogService;
        private readonly UserService _userService;

        public ComputadoresController(IConfiguration configuration, ILogger<ComputadoresController> logger, PersistentLogService persistentLogService, UserService userService)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
            _persistentLogService = persistentLogService;
            _userService = userService;
        }

        public async Task<IActionResult> Index(string sortOrder, string searchString,
            List<string> currentFabricantes, List<string> currentSOs, List<string> currentProcessadorFabricantes, List<string> currentRamTipos, List<string> currentProcessadores, List<string> currentRams,
            int pageNumber = 1, int pageSize = 25)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["IpSortParm"] = String.IsNullOrEmpty(sortOrder) ? "ip_desc" : "";
            ViewData["MacSortParm"] = sortOrder == "mac" ? "mac_desc" : "mac";
            ViewData["UserSortParm"] = sortOrder == "user" ? "user_desc" : "user";
            ViewData["HostnameSortParm"] = sortOrder == "hostname" ? "hostname_desc" : "hostname";
            ViewData["OsSortParm"] = sortOrder == "os" ? "os_desc" : "os";
            ViewData["DateSortParm"] = sortOrder == "date" ? "date_desc" : "date";
            ViewData["CurrentFilter"] = searchString;

            var viewModel = new ComputadorIndexViewModel
            {
                Computadores = new List<Computador>(),
                PageNumber = pageNumber,
                PageSize = pageSize,
                SearchString = searchString,
                CurrentSort = sortOrder,
                CurrentFabricantes = currentFabricantes,
                CurrentSOs = currentSOs,
                CurrentProcessadorFabricantes = currentProcessadorFabricantes,
                CurrentRamTipos = currentRamTipos,
                CurrentProcessadores = currentProcessadores,
                CurrentRams = currentRams
            };

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    viewModel.Fabricantes = GetDistinctComputerValues(connection, "Fabricante");
                    viewModel.SOs = GetDistinctComputerValues(connection, "SO");
                    viewModel.ProcessadorFabricantes = GetDistinctComputerValues(connection, "ProcessadorFabricante");
                    viewModel.RamTipos = GetDistinctComputerValues(connection, "RamTipo");
                    viewModel.Processadores = GetDistinctComputerValues(connection, "Processador");
                    viewModel.Rams = GetDistinctComputerValues(connection, "Ram");

                    var whereClauses = new List<string>();
                    var parameters = new Dictionary<string, object>();

                    if (!string.IsNullOrEmpty(searchString))
                    {
                        whereClauses.Add("(c.IP LIKE @search OR c.MAC LIKE @search OR u.Nome LIKE @search OR c.Hostname LIKE @search)");
                        parameters.Add("@search", $"%{searchString}%");
                    }

                    Action<string, List<string>> addInClause = (columnName, values) =>
                    {
                        if (values != null && values.Any())
                        {
                            var paramNames = new List<string>();
                            for (int i = 0; i < values.Count; i++)
                            {
                                var paramName = $"@{columnName.ToLower()}{i}";
                                paramNames.Add(paramName);
                                parameters.Add(paramName, values[i]);
                            }
                            whereClauses.Add($"c.{columnName} IN ({string.Join(", ", paramNames)})");
                        }
                    };

                    addInClause("Fabricante", currentFabricantes);
                    addInClause("SO", currentSOs);
                    addInClause("ProcessadorFabricante", currentProcessadorFabricantes);
                    addInClause("RamTipo", currentRamTipos);
                    addInClause("Processador", currentProcessadores);
                    addInClause("Ram", currentRams);

                    var user = await _userService.FindByLoginAsync(User.Identity.Name);
                    if (User.IsInRole("Coordenador"))
                    {
                        var users = await _userService.GetUsersBySupervisorAsync(user.Id);
                        var userIds = users.Select(u => u.Id).ToList();
                        userIds.Add(user.Id);

                        var idParams = new List<string>();
                        for (int i = 0; i < userIds.Count; i++)
                        {
                            var paramName = $"@userId{i}";
                            idParams.Add(paramName);
                            parameters.Add(paramName, userIds[i]);
                        }
                        whereClauses.Add($"c.UserId IN ({string.Join(", ", idParams)})");
                    }

                    string whereSql = whereClauses.Any() ? $"WHERE {string.Join(" AND ", whereClauses)}" : "";

                    // Get total count
                    string countSql = $"SELECT COUNT(c.MAC) FROM Computadores c LEFT JOIN Users u ON c.UserId = u.Id {whereSql}";
                    using (var countCommand = new SqlCommand(countSql, connection))
                    {
                        foreach (var p in parameters) countCommand.Parameters.AddWithValue(p.Key, p.Value);
                        viewModel.TotalCount = (int)countCommand.ExecuteScalar();
                    }

                    // Get paginated data
                    string orderBySql;
                    switch (sortOrder)
                    {
                        case "ip_desc": orderBySql = "ORDER BY c.IP DESC"; break;
                        case "mac": orderBySql = "ORDER BY c.MAC"; break;
                        case "mac_desc": orderBySql = "ORDER BY c.MAC DESC"; break;
                        case "user": orderBySql = "ORDER BY u.Nome"; break;
                        case "user_desc": orderBySql = "ORDER BY u.Nome DESC"; break;
                        case "hostname": orderBySql = "ORDER BY c.Hostname"; break;
                        case "hostname_desc": orderBySql = "ORDER BY c.Hostname DESC"; break;
                        case "os": orderBySql = "ORDER BY c.SO"; break;
                        case "os_desc": orderBySql = "ORDER BY c.SO DESC"; break;
                        case "date": orderBySql = "ORDER BY c.DataColeta"; break;
                        case "date_desc": orderBySql = "ORDER BY c.DataColeta DESC"; break;
                        default: orderBySql = "ORDER BY c.IP"; break;
                    }

                    string sql = $"SELECT c.MAC, c.IP, u.Nome as ColaboradorNome, c.Hostname, c.Fabricante, c.Processador, c.ProcessadorFabricante, c.ProcessadorCore, c.ProcessadorThread, c.ProcessadorClock, c.Ram, c.RamTipo, c.RamVelocidade, c.RamVoltagem, c.RamPorModule, c.ArmazenamentoC, c.ArmazenamentoCTotal, c.ArmazenamentoCLivre, c.ArmazenamentoD, c.ArmazenamentoDTotal, c.ArmazenamentoDLivre, c.ConsumoCPU, c.SO, c.DataColeta, c.UserId FROM Computadores c LEFT JOIN Users u ON c.UserId = u.Id {whereSql} {orderBySql} OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";

                    using (SqlCommand cmd = new SqlCommand(sql, connection))
                    {
                        foreach (var p in parameters) cmd.Parameters.AddWithValue(p.Key, p.Value);
                        cmd.Parameters.AddWithValue("@offset", (pageNumber - 1) * pageSize);
                        cmd.Parameters.AddWithValue("@pageSize", pageSize);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                viewModel.Computadores.Add(new Computador
                                {
                                    MAC = reader["MAC"].ToString(),
                                    IP = reader["IP"].ToString(),
                                    UserId = reader["UserId"] != DBNull.Value ? Convert.ToInt32(reader["UserId"]) : (int?)null,
                                    ColaboradorNome = reader["ColaboradorNome"].ToString(),
                                    Hostname = reader["Hostname"].ToString(),
                                    Fabricante = reader["Fabricante"].ToString(),
                                    Processador = reader["Processador"].ToString(),
                                    ProcessadorFabricante = reader["ProcessadorFabricante"].ToString(),
                                    ProcessadorCore = reader["ProcessadorCore"].ToString(),
                                    ProcessadorThread = reader["ProcessadorThread"].ToString(),
                                    ProcessadorClock = reader["ProcessadorClock"].ToString(),
                                    Ram = reader["Ram"].ToString(),
                                    RamTipo = reader["RamTipo"].ToString(),
                                    RamVelocidade = reader["RamVelocidade"].ToString(),
                                    RamVoltagem = reader["RamVoltagem"].ToString(),
                                    RamPorModule = reader["RamPorModule"].ToString(),
                                    ArmazenamentoC = reader["ArmazenamentoC"].ToString(),
                                    ArmazenamentoCTotal = reader["ArmazenamentoCTotal"].ToString(),
                                    ArmazenamentoCLivre = reader["ArmazenamentoCLivre"].ToString(),
                                    ArmazenamentoD = reader["ArmazenamentoD"].ToString(),
                                    ArmazenamentoDTotal = reader["ArmazenamentoDTotal"].ToString(),
                                    ArmazenamentoDLivre = reader["ArmazenamentoDLivre"].ToString(),
                                    ConsumoCPU = reader["ConsumoCPU"].ToString(),
                                    SO = reader["SO"].ToString(),
                                    DataColeta = reader["DataColeta"] != DBNull.Value ? Convert.ToDateTime(reader["DataColeta"]) : (DateTime?)null
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter a lista de computadores.");
                ViewBag.Message = "Ocorreu um erro ao obter a lista de computadores. Por favor, tente novamente mais tarde.";
            }

            return View(viewModel);
        }

        private List<string> GetDistinctComputerValues(SqlConnection connection, string columnName)
        {
            var values = new List<string>();
            // Use a separate command to prevent issues with open readers
            using (var command = new SqlCommand($"SELECT DISTINCT {columnName} FROM Computadores WHERE {columnName} IS NOT NULL ORDER BY {columnName}", connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        values.Add(reader.GetString(0));
                    }
                }
            }
            return values;
        }

        // GET: Computadores/Create
        [Authorize(Roles = "Admin,Coordenador")]
        public async Task<IActionResult> Create()
        {
            ViewData["UserId"] = new SelectList(await _userService.GetAllUsersAsync(), "Id", "Nome");
            return View(new ComputadorViewModel());
        }

        // POST: Computadores/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Coordenador")]
        public async Task<IActionResult> Create(ComputadorViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (viewModel.UserId.HasValue)
                    {
                        var user = await _userService.FindByIdAsync(viewModel.UserId.Value);
                        viewModel.ColaboradorNome = user?.Nome;
                    }
                    else
                    {
                        viewModel.ColaboradorNome = null;
                    }

                    using (SqlConnection connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();

                        string sql = "INSERT INTO Computadores (MAC, IP, UserId, Hostname, Fabricante, Processador, ProcessadorFabricante, ProcessadorCore, ProcessadorThread, ProcessadorClock, Ram, RamTipo, RamVelocidade, RamVoltagem, RamPorModule, ArmazenamentoC, ArmazenamentoCTotal, ArmazenamentoCLivre, ArmazenamentoD, ArmazenamentoDTotal, ArmazenamentoDLivre, ConsumoCPU, SO, DataColeta) VALUES (@MAC, @IP, @UserId, @Hostname, @Fabricante, @Processador, @ProcessadorFabricante, @ProcessadorCore, @ProcessadorThread, @ProcessadorClock, @Ram, @RamTipo, @RamVelocidade, @RamVoltagem, @RamPorModule, @ArmazenamentoC, @ArmazenamentoCTotal, @ArmazenamentoCLivre, @ArmazenamentoD, @ArmazenamentoDTotal, @ArmazenamentoDLivre, @ConsumoCPU, @SO, @DataColeta)";

                        using (SqlCommand cmd = new SqlCommand(sql, connection))
                        {
                            cmd.Parameters.AddWithValue("@MAC", viewModel.MAC);
                            cmd.Parameters.AddWithValue("@IP", (object)viewModel.IP ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@UserId", (object)viewModel.UserId ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Hostname", (object)viewModel.Hostname ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Fabricante", (object)viewModel.Fabricante ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Processador", (object)viewModel.Processador ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ProcessadorFabricante", (object)viewModel.ProcessadorFabricante ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ProcessadorCore", (object)viewModel.ProcessadorCore ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ProcessadorThread", (object)viewModel.ProcessadorThread ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ProcessadorClock", (object)viewModel.ProcessadorClock ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Ram", (object)viewModel.Ram ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@RamTipo", (object)viewModel.RamTipo ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@RamVelocidade", (object)viewModel.RamVelocidade ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@RamVoltagem", (object)viewModel.RamVoltagem ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@RamPorModule", (object)viewModel.RamPorModule ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ArmazenamentoC", (object)viewModel.ArmazenamentoC ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ArmazenamentoCTotal", (object)viewModel.ArmazenamentoCTotal ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ArmazenamentoCLivre", (object)viewModel.ArmazenamentoCLivre ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ArmazenamentoD", (object)viewModel.ArmazenamentoD ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ArmazenamentoDTotal", (object)viewModel.ArmazenamentoDTotal ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ArmazenamentoDLivre", (object)viewModel.ArmazenamentoDLivre ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ConsumoCPU", (object)viewModel.ConsumoCPU ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@SO", (object)viewModel.SO ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@DataColeta", DateTime.Now);

                            cmd.ExecuteNonQuery();
                        }
                    }
                    _persistentLogService.AddLog("Computer", "Create", User.Identity.Name, $"Computer '{viewModel.MAC}' created.");
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao criar um novo computador.");
                    ModelState.AddModelError(string.Empty, "Ocorreu um erro ao criar o computador. Verifique se o MAC já existe.");
                }
            }
            ViewData["UserId"] = new SelectList(await _userService.GetAllUsersAsync(), "Id", "Nome", viewModel.UserId);
            return View(viewModel);
        }

        // GET: Computadores/Edit/5
        [Authorize(Roles = "Admin,Coordenador")]
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Computador computador = FindComputadorById(id);

            if (computador == null)
            {
                return NotFound();
            }

            var viewModel = new ComputadorViewModel
            {
                MAC = computador.MAC,
                IP = computador.IP,
                UserId = computador.UserId,
                ColaboradorNome = computador.ColaboradorNome,
                Hostname = computador.Hostname,
                Fabricante = computador.Fabricante,
                Processador = computador.Processador,
                ProcessadorFabricante = computador.ProcessadorFabricante,
                ProcessadorCore = computador.ProcessadorCore,
                ProcessadorThread = computador.ProcessadorThread,
                ProcessadorClock = computador.ProcessadorClock,
                Ram = computador.Ram,
                RamTipo = computador.RamTipo,
                RamVelocidade = computador.RamVelocidade,
                RamVoltagem = computador.RamVoltagem,
                RamPorModule = computador.RamPorModule,
                ArmazenamentoC = computador.ArmazenamentoC,
                ArmazenamentoCTotal = computador.ArmazenamentoCTotal,
                ArmazenamentoCLivre = computador.ArmazenamentoCLivre,
                ArmazenamentoD = computador.ArmazenamentoD,
                ArmazenamentoDTotal = computador.ArmazenamentoDTotal,
                ArmazenamentoDLivre = computador.ArmazenamentoDLivre,
                ConsumoCPU = computador.ConsumoCPU,
                SO = computador.SO
            };
            ViewData["UserId"] = new SelectList(await _userService.GetAllUsersAsync(), "Id", "Nome", viewModel.UserId);
            return View(viewModel);
        }

        // POST: Computadores/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Coordenador")]
        public async Task<IActionResult> Edit(string id, ComputadorViewModel viewModel)
        {
            if (id != viewModel.MAC)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (viewModel.UserId.HasValue)
                    {
                        var user = await _userService.FindByIdAsync(viewModel.UserId.Value);
                        viewModel.ColaboradorNome = user?.Nome;
                    }
                    else
                    {
                        viewModel.ColaboradorNome = null;
                    }

                    using (SqlConnection connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();

                        string sql = "UPDATE Computadores SET IP = @IP, UserId = @UserId, Hostname = @Hostname, Fabricante = @Fabricante, Processador = @Processador, ProcessadorFabricante = @ProcessadorFabricante, ProcessadorCore = @ProcessadorCore, ProcessadorThread = @ProcessadorThread, ProcessadorClock = @ProcessadorClock, Ram = @Ram, RamTipo = @RamTipo, RamVelocidade = @RamVelocidade, RamVoltagem = @RamVoltagem, RamPorModule = @RamPorModule, ArmazenamentoC = @ArmazenamentoC, ArmazenamentoCTotal = @ArmazenamentoCTotal, ArmazenamentoCLivre = @ArmazenamentoCLivre, ArmazenamentoD = @ArmazenamentoD, ArmazenamentoDTotal = @ArmazenamentoDTotal, ArmazenamentoDLivre = @ArmazenamentoDLivre, ConsumoCPU = @ConsumoCPU, SO = @SO WHERE MAC = @MAC";

                        using (SqlCommand cmd = new SqlCommand(sql, connection))
                        {
                            cmd.Parameters.AddWithValue("@MAC", viewModel.MAC);
                            cmd.Parameters.AddWithValue("@IP", (object)viewModel.IP ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@UserId", (object)viewModel.UserId ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Hostname", (object)viewModel.Hostname ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Fabricante", (object)viewModel.Fabricante ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Processador", (object)viewModel.Processador ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ProcessadorFabricante", (object)viewModel.ProcessadorFabricante ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ProcessadorCore", (object)viewModel.ProcessadorCore ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ProcessadorThread", (object)viewModel.ProcessadorThread ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ProcessadorClock", (object)viewModel.ProcessadorClock ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Ram", (object)viewModel.Ram ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@RamTipo", (object)viewModel.RamTipo ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@RamVelocidade", (object)viewModel.RamVelocidade ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@RamVoltagem", (object)viewModel.RamVoltagem ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@RamPorModule", (object)viewModel.RamPorModule ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ArmazenamentoC", (object)viewModel.ArmazenamentoC ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ArmazenamentoCTotal", (object)viewModel.ArmazenamentoCTotal ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ArmazenamentoCLivre", (object)viewModel.ArmazenamentoCLivre ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ArmazenamentoD", (object)viewModel.ArmazenamentoD ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ArmazenamentoDTotal", (object)viewModel.ArmazenamentoDTotal ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ArmazenamentoDLivre", (object)viewModel.ArmazenamentoDLivre ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ConsumoCPU", (object)viewModel.ConsumoCPU ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@SO", (object)viewModel.SO ?? DBNull.Value);

                            cmd.ExecuteNonQuery();
                        }
                    }
                    _persistentLogService.AddLog("Computer", "Update", User.Identity.Name, $"Computer '{viewModel.MAC}' updated.");
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao editar o computador.");
                    ModelState.AddModelError(string.Empty, "Ocorreu um erro ao editar o computador.");
                }
            }
            ViewData["UserId"] = new SelectList(await _userService.GetAllUsersAsync(), "Id", "Nome", viewModel.UserId);
            return View(viewModel);
        }

        // GET: Computadores/Delete/5
        [Authorize(Roles = "Admin,Coordenador")]
        public IActionResult Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Computador computador = FindComputadorById(id);

            if (computador == null)
            {
                return NotFound();
            }

            return View(computador);
        }

        // POST: Computadores/Delete/5
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

                    string sql = "DELETE FROM Computadores WHERE MAC = @MAC";

                    using (SqlCommand cmd = new SqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@MAC", id);
                        cmd.ExecuteNonQuery();
                    }
                }
                _persistentLogService.AddLog("Computer", "Delete", User.Identity.Name, $"Computer '{id}' deleted.");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao excluir o computador.");
                ViewBag.Message = "Ocorreu um erro ao excluir o computador. Por favor, tente novamente mais tarde.";
                return View();
            }
        }

        private Computador FindComputadorById(string id)
        {
            Computador computador = null;
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    string sql = "SELECT c.MAC, c.IP, c.UserId, u.Nome as ColaboradorNome, c.Hostname, c.Fabricante, c.Processador, c.ProcessadorFabricante, c.ProcessadorCore, c.ProcessadorThread, c.ProcessadorClock, c.Ram, c.RamTipo, c.RamVelocidade, c.RamVoltagem, c.RamPorModule, c.ArmazenamentoC, c.ArmazenamentoCTotal, c.ArmazenamentoCLivre, c.ArmazenamentoD, c.ArmazenamentoDTotal, c.ArmazenamentoDLivre, c.ConsumoCPU, c.SO, c.DataColeta FROM Computadores c LEFT JOIN Users u ON c.UserId = u.Id WHERE c.MAC = @MAC";

                    using (SqlCommand cmd = new SqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@MAC", id);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                computador = new Computador
                                {
                                    MAC = reader["MAC"].ToString(),
                                    IP = reader["IP"].ToString(),
                                    UserId = reader["UserId"] != DBNull.Value ? Convert.ToInt32(reader["UserId"]) : (int?)null,
                                    ColaboradorNome = reader["ColaboradorNome"].ToString(),
                                    Hostname = reader["Hostname"].ToString(),
                                    Fabricante = reader["Fabricante"].ToString(),
                                    Processador = reader["Processador"].ToString(),
                                    ProcessadorFabricante = reader["ProcessadorFabricante"].ToString(),
                                    ProcessadorCore = reader["ProcessadorCore"].ToString(),
                                    ProcessadorThread = reader["ProcessadorThread"].ToString(),
                                    ProcessadorClock = reader["ProcessadorClock"].ToString(),
                                    Ram = reader["Ram"].ToString(),
                                    RamTipo = reader["RamTipo"].ToString(),
                                    RamVelocidade = reader["RamVelocidade"].ToString(),
                                    RamVoltagem = reader["RamVoltagem"].ToString(),
                                    RamPorModule = reader["RamPorModule"].ToString(),
                                    ArmazenamentoC = reader["ArmazenamentoC"].ToString(),
                                    ArmazenamentoCTotal = reader["ArmazenamentoCTotal"].ToString(),
                                    ArmazenamentoCLivre = reader["ArmazenamentoCLivre"].ToString(),
                                    ArmazenamentoD = reader["ArmazenamentoD"].ToString(),
                                    ArmazenamentoDTotal = reader["ArmazenamentoDTotal"].ToString(),
                                    ArmazenamentoDLivre = reader["ArmazenamentoDLivre"].ToString(),
                                    ConsumoCPU = reader["ConsumoCPU"].ToString(),
                                    SO = reader["SO"].ToString(),
                                    DataColeta = reader["DataColeta"] != DBNull.Value ? Convert.ToDateTime(reader["DataColeta"]) : (DateTime?)null
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter os detalhes do computador.");
                ViewBag.Message = "Ocorreu um erro ao obter os detalhes do computador. Por favor, tente novamente mais tarde.";
            }
            return computador;
        }

    }
}