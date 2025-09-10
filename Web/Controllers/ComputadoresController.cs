using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Web.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Web.Services;
using System.Security.Claims;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin,Coordenador,Colaborador,Diretoria")]
    public class ComputadoresController : Controller
    {
        private readonly string _connectionString;
        private readonly ILogger<ComputadoresController> _logger;
        private readonly PersistentLogService _persistentLogService;

        public ComputadoresController(IConfiguration configuration, ILogger<ComputadoresController> logger, PersistentLogService persistentLogService)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
            _persistentLogService = persistentLogService;
        }

        public IActionResult Index(string sortOrder, string searchString,
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
                    var userCpf = User.FindFirstValue("ColaboradorCPF");

                    string baseSql = @"
                        FROM Computadores comp
                        LEFT JOIN Colaboradores col ON comp.ColaboradorCPF = col.CPF
                    ";

                    if (User.IsInRole("Colaborador") && !User.IsInRole("Admin") && !User.IsInRole("Diretoria"))
                    {
                        whereClauses.Add("comp.ColaboradorCPF = @UserCpf");
                        parameters.Add("@UserCpf", (object)userCpf ?? DBNull.Value);
                    }
                    else if (User.IsInRole("Coordenador") && !User.IsInRole("Admin") && !User.IsInRole("Diretoria"))
                    {
                        whereClauses.Add("(col.CoordenadorCPF = @UserCpf OR comp.ColaboradorCPF = @UserCpf)");
                        parameters.Add("@UserCpf", (object)userCpf ?? DBNull.Value);
                    }


                    if (!string.IsNullOrEmpty(searchString))
                    {
                        whereClauses.Add("(comp.IP LIKE @search OR comp.MAC LIKE @search OR col.Nome LIKE @search OR comp.Hostname LIKE @search)");
                        parameters.Add("@search", $"%{searchString}%");
                    }

                    Action<string, List<string>> addInClause = (columnName, values) =>
                    {
                        if (values != null && values.Any())
                        {
                            var paramNames = new List<string>();
                            for (int i = 0; i < values.Count; i++)
                            {
                                var paramName = $"@{columnName.ToLower().Replace(".", "")}{i}";
                                paramNames.Add(paramName);
                                parameters.Add(paramName, values[i]);
                            }
                            whereClauses.Add($"{columnName} IN ({string.Join(", ", paramNames)})");
                        }
                    };

                    addInClause("comp.Fabricante", currentFabricantes);
                    addInClause("comp.SO", currentSOs);
                    addInClause("comp.ProcessadorFabricante", currentProcessadorFabricantes);
                    addInClause("comp.RamTipo", currentRamTipos);
                    addInClause("comp.Processador", currentProcessadores);
                    addInClause("comp.Ram", currentRams);

                    string whereSql = whereClauses.Any() ? $"WHERE {string.Join(" AND ", whereClauses)}" : "";

                    string countSql = $"SELECT COUNT(comp.MAC) {baseSql} {whereSql}";
                    using (var countCommand = new SqlCommand(countSql, connection))
                    {
                        foreach (var p in parameters) countCommand.Parameters.AddWithValue(p.Key, p.Value);
                        viewModel.TotalCount = (int)countCommand.ExecuteScalar();
                    }

                    string orderBySql;
                    switch (sortOrder)
                    {
                        case "ip_desc": orderBySql = "ORDER BY comp.IP DESC"; break;
                        case "mac": orderBySql = "ORDER BY comp.MAC"; break;
                        case "mac_desc": orderBySql = "ORDER BY comp.MAC DESC"; break;
                        case "user": orderBySql = "ORDER BY col.Nome"; break;
                        case "user_desc": orderBySql = "ORDER BY col.Nome DESC"; break;
                        case "hostname": orderBySql = "ORDER BY comp.Hostname"; break;
                        case "hostname_desc": orderBySql = "ORDER BY comp.Hostname DESC"; break;
                        case "os": orderBySql = "ORDER BY comp.SO"; break;
                        case "os_desc": orderBySql = "ORDER BY comp.SO DESC"; break;
                        case "date": orderBySql = "ORDER BY comp.DataColeta"; break;
                        case "date_desc": orderBySql = "ORDER BY comp.DataColeta DESC"; break;
                        default: orderBySql = "ORDER BY comp.IP"; break;
                    }

                    // --- CORREÇÃO APLICADA AQUI ---
                    string sqlFields = @"
                        SELECT
                            comp.MAC, comp.IP, comp.ColaboradorCPF, col.Nome as ColaboradorNome, comp.Hostname,
                            comp.Fabricante, comp.Processador, comp.ProcessadorFabricante, comp.ProcessadorCore,
                            comp.ProcessadorThread, comp.ProcessadorClock, comp.Ram, comp.RamTipo,
                            comp.RamVelocidade, comp.RamVoltagem, comp.RamPorModule, comp.ArmazenamentoC,
                            comp.ArmazenamentoCTotal, comp.ArmazenamentoCLivre, comp.ArmazenamentoD,
                            comp.ArmazenamentoDTotal, comp.ArmazenamentoDLivre, comp.ConsumoCPU, comp.SO,
                            comp.DataColeta
                    ";
                    string sql = $"{sqlFields} {baseSql} {whereSql} {orderBySql} OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";
                    // --- FIM DA CORREÇÃO ---

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
                                    ColaboradorCPF = reader["ColaboradorCPF"] != DBNull.Value ? reader["ColaboradorCPF"].ToString() : null,
                                    ColaboradorNome = reader["ColaboradorNome"] != DBNull.Value ? reader["ColaboradorNome"].ToString() : null,
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

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewData["Colaboradores"] = new SelectList(GetColaboradores(), "CPF", "Nome");
            return View(new ComputadorViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult Create(ComputadorViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();

                        string sql = "INSERT INTO Computadores (MAC, IP, ColaboradorCPF, Hostname, Fabricante, Processador, ProcessadorFabricante, ProcessadorCore, ProcessadorThread, ProcessadorClock, Ram, RamTipo, RamVelocidade, RamVoltagem, RamPorModule, ArmazenamentoC, ArmazenamentoCTotal, ArmazenamentoCLivre, ArmazenamentoD, ArmazenamentoDTotal, ArmazenamentoDLivre, ConsumoCPU, SO, DataColeta) VALUES (@MAC, @IP, @ColaboradorCPF, @Hostname, @Fabricante, @Processador, @ProcessadorFabricante, @ProcessadorCore, @ProcessadorThread, @ProcessadorClock, @Ram, @RamTipo, @RamVelocidade, @RamVoltagem, @RamPorModule, @ArmazenamentoC, @ArmazenamentoCTotal, @ArmazenamentoCLivre, @ArmazenamentoD, @ArmazenamentoDTotal, @ArmazenamentoDLivre, @ConsumoCPU, @SO, @DataColeta)";

                        using (SqlCommand cmd = new SqlCommand(sql, connection))
                        {
                            cmd.Parameters.AddWithValue("@MAC", viewModel.MAC);
                            cmd.Parameters.AddWithValue("@IP", (object)viewModel.IP ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ColaboradorCPF", (object)viewModel.ColaboradorCPF ?? DBNull.Value);
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
            ViewData["Colaboradores"] = new SelectList(GetColaboradores(), "CPF", "Nome", viewModel.ColaboradorCPF);
            return View(viewModel);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Edit(string id)
        {
            if (id == null) return NotFound();
            Computador computador = FindComputadorById(id);
            if (computador == null) return NotFound();

            var viewModel = new ComputadorViewModel
            {
                MAC = computador.MAC,
                IP = computador.IP,
                ColaboradorCPF = computador.ColaboradorCPF,
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
            ViewData["Colaboradores"] = new SelectList(GetColaboradores(), "CPF", "Nome", viewModel.ColaboradorCPF);
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult Edit(string id, ComputadorViewModel viewModel)
        {
            if (id != viewModel.MAC) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(_connectionString))
                    {
                        connection.Open();
                        string sql = "UPDATE Computadores SET IP = @IP, ColaboradorCPF = @ColaboradorCPF, Hostname = @Hostname, Fabricante = @Fabricante, Processador = @Processador, ProcessadorFabricante = @ProcessadorFabricante, ProcessadorCore = @ProcessadorCore, ProcessadorThread = @ProcessadorThread, ProcessadorClock = @ProcessadorClock, Ram = @Ram, RamTipo = @RamTipo, RamVelocidade = @RamVelocidade, RamVoltagem = @RamVoltagem, RamPorModule = @RamPorModule, ArmazenamentoC = @ArmazenamentoC, ArmazenamentoCTotal = @ArmazenamentoCTotal, ArmazenamentoCLivre = @ArmazenamentoCLivre, ArmazenamentoD = @ArmazenamentoD, ArmazenamentoDTotal = @ArmazenamentoDTotal, ArmazenamentoDLivre = @ArmazenamentoDLivre, ConsumoCPU = @ConsumoCPU, SO = @SO WHERE MAC = @MAC";

                        using (SqlCommand cmd = new SqlCommand(sql, connection))
                        {
                            cmd.Parameters.AddWithValue("@MAC", viewModel.MAC);
                            cmd.Parameters.AddWithValue("@IP", (object)viewModel.IP ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ColaboradorCPF", (object)viewModel.ColaboradorCPF ?? DBNull.Value);
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
            ViewData["Colaboradores"] = new SelectList(GetColaboradores(), "CPF", "Nome", viewModel.ColaboradorCPF);
            return View(viewModel);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Delete(string id)
        {
            if (id == null) return NotFound();
            Computador computador = FindComputadorById(id);
            if (computador == null) return NotFound();
            return View(computador);
        }

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
                Computador computador = FindComputadorById(id);
                if (computador == null)
                {
                    return NotFound();
                }
                return View(computador);
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
                    string sql = "SELECT comp.*, col.Nome AS ColaboradorNome FROM Computadores comp LEFT JOIN Colaboradores col ON comp.ColaboradorCPF = col.CPF WHERE comp.MAC = @MAC";
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
                                    ColaboradorCPF = reader["ColaboradorCPF"] != DBNull.Value ? reader["ColaboradorCPF"].ToString() : null,
                                    ColaboradorNome = reader["ColaboradorNome"] != DBNull.Value ? reader["ColaboradorNome"].ToString() : null,
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