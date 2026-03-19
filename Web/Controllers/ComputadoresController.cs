using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Web.Models;
using System.Data;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Web.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using System.IO;
using System.Threading.Tasks;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin,Coordenador,Colaborador,Diretoria")]
    public class ComputadoresController : Controller
    {
        private readonly IDatabaseService _databaseService;
        private readonly ILogger<ComputadoresController> _logger;
        private readonly PersistentLogService _persistentLogService;

        public ComputadoresController(IDatabaseService databaseService, IConfiguration configuration, ILogger<ComputadoresController> logger, PersistentLogService persistentLogService)
        {
            _databaseService = databaseService;
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
                using (var connection = _databaseService.CreateConnection())
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
                    using (var countCommand = connection.CreateCommand())
                    {
                        countCommand.CommandText = countSql;
                        foreach (var p in parameters) {
                            var param = countCommand.CreateParameter();
                            param.ParameterName = p.Key;
                            param.Value = p.Value;
                            countCommand.Parameters.Add(param);
                        }
                        var countResult = countCommand.ExecuteScalar();
                        viewModel.TotalCount = countResult != DBNull.Value ? Convert.ToInt32(countResult) : 0;
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
                            comp.DataColeta, comp.PartNumber
                    ";
                    string sql = $"{sqlFields} {baseSql} {whereSql} {orderBySql} LIMIT @pageSize OFFSET @offset";
                    // --- FIM DA CORREÇÃO ---

                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = sql;
                        foreach (var p in parameters) {
                            var param = cmd.CreateParameter();
                            param.ParameterName = p.Key;
                            param.Value = p.Value;
                            cmd.Parameters.Add(param);
                        }

                        var pOffset = cmd.CreateParameter(); pOffset.ParameterName = "@offset"; pOffset.Value = (pageNumber - 1) * pageSize; cmd.Parameters.Add(pOffset);
                        var pPageSize = cmd.CreateParameter(); pPageSize.ParameterName = "@pageSize"; pPageSize.Value = pageSize; cmd.Parameters.Add(pPageSize);

                        using (var reader = cmd.ExecuteReader())
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
                                    DataColeta = reader["DataColeta"] != DBNull.Value ? Convert.ToDateTime(reader["DataColeta"]) : (DateTime?)null,
                                    PartNumber = reader["PartNumber"].ToString()
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

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Importar(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Nenhum arquivo selecionado.";
                return RedirectToAction(nameof(Index));
            }

            var computadores = new List<Computador>();
            try
            {
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    using (var package = new ExcelPackage(stream))
                    {
                        ExcelWorksheet worksheet = package.Workbook.Worksheets.FirstOrDefault();
                        if (worksheet == null)
                        {
                            TempData["ErrorMessage"] = "A planilha do Excel está vazia ou não foi encontrada.";
                            return RedirectToAction(nameof(Index));
                        }

                        int rowCount = worksheet.Dimension.Rows;
                        for (int row = 2; row <= rowCount; row++)
                        {
                            var sanitizedCpf = SanitizeCpf(worksheet.Cells[row, 3].Value?.ToString().Trim());
                            var computador = new Computador
                            {
                                MAC = worksheet.Cells[row, 1].Value?.ToString().Trim(),
                                IP = worksheet.Cells[row, 2].Value?.ToString().Trim(),
                                ColaboradorCPF = string.IsNullOrEmpty(sanitizedCpf) ? null : sanitizedCpf,
                                Hostname = worksheet.Cells[row, 4].Value?.ToString().Trim(),
                                Fabricante = worksheet.Cells[row, 5].Value?.ToString().Trim(),
                                Processador = worksheet.Cells[row, 6].Value?.ToString().Trim(),
                                ProcessadorFabricante = worksheet.Cells[row, 7].Value?.ToString().Trim(),
                                ProcessadorCore = worksheet.Cells[row, 8].Value?.ToString().Trim(),
                                ProcessadorThread = worksheet.Cells[row, 9].Value?.ToString().Trim(),
                                ProcessadorClock = worksheet.Cells[row, 10].Value?.ToString().Trim(),
                                Ram = worksheet.Cells[row, 11].Value?.ToString().Trim(),
                                RamTipo = worksheet.Cells[row, 12].Value?.ToString().Trim(),
                                RamVelocidade = worksheet.Cells[row, 13].Value?.ToString().Trim(),
                                RamVoltagem = worksheet.Cells[row, 14].Value?.ToString().Trim(),
                                RamPorModule = worksheet.Cells[row, 15].Value?.ToString().Trim(),
                                ArmazenamentoC = worksheet.Cells[row, 16].Value?.ToString().Trim(),
                                ArmazenamentoCTotal = worksheet.Cells[row, 17].Value?.ToString().Trim(),
                                ArmazenamentoCLivre = worksheet.Cells[row, 18].Value?.ToString().Trim(),
                                ArmazenamentoD = worksheet.Cells[row, 19].Value?.ToString().Trim(),
                                ArmazenamentoDTotal = worksheet.Cells[row, 20].Value?.ToString().Trim(),
                                ArmazenamentoDLivre = worksheet.Cells[row, 21].Value?.ToString().Trim(),
                                ConsumoCPU = worksheet.Cells[row, 22].Value?.ToString().Trim(),
                                SO = worksheet.Cells[row, 23].Value?.ToString().Trim(),
                                PartNumber = worksheet.Cells[row, 24].Value?.ToString().Trim()
                            };

                            if (!string.IsNullOrWhiteSpace(computador.MAC))
                            {
                                computadores.Add(computador);
                            }
                        }
                    }
                }

                int adicionados = 0;
                int atualizados = 0;
                var invalidCpfs = new List<string>();

                using (var connection = _databaseService.CreateConnection())
                {
                    connection.Open();

                    var colaboradoresCpf = new HashSet<string>();
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "SELECT CPF FROM Colaboradores";
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                colaboradoresCpf.Add(reader.GetString(0));
                            }
                        }
                    }

                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            foreach (var computador in computadores)
                            {
                                if (!string.IsNullOrEmpty(computador.ColaboradorCPF) && !colaboradoresCpf.Contains(computador.ColaboradorCPF))
                                {
                                    invalidCpfs.Add(computador.MAC);
                                    computador.ColaboradorCPF = null;
                                }

                                var existente = FindComputadorById(computador.MAC, connection, transaction);
                                if (existente != null)
                                {
                                    string updateSql = @"UPDATE Computadores SET
                                                       IP = @IP, ColaboradorCPF = @ColaboradorCPF, Hostname = @Hostname, Fabricante = @Fabricante,
                                                       Processador = @Processador, ProcessadorFabricante = @ProcessadorFabricante, ProcessadorCore = @ProcessadorCore,
                                                       ProcessadorThread = @ProcessadorThread, ProcessadorClock = @ProcessadorClock, Ram = @Ram,
                                                       RamTipo = @RamTipo, RamVelocidade = @RamVelocidade, RamVoltagem = @RamVoltagem,
                                                       RamPorModule = @RamPorModule, ArmazenamentoC = @ArmazenamentoC, ArmazenamentoCTotal = @ArmazenamentoCTotal,
                                                       ArmazenamentoCLivre = @ArmazenamentoCLivre, ArmazenamentoD = @ArmazenamentoD, ArmazenamentoDTotal = @ArmazenamentoDTotal,
                                                       ArmazenamentoDLivre = @ArmazenamentoDLivre, ConsumoCPU = @ConsumoCPU, SO = @SO, DataColeta = @DataColeta, PartNumber = @PartNumber
                                                       WHERE MAC = @MAC";
                                    using (var cmd = connection.CreateCommand())
                                    {
                                        cmd.Transaction = transaction;
                                        cmd.CommandText = updateSql;
                                        AddComputadorParameters(cmd, computador);
                                        var pDate = cmd.CreateParameter(); pDate.ParameterName = "@DataColeta"; pDate.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); cmd.Parameters.Add(pDate);
                                        cmd.ExecuteNonQuery();
                                    }
                                    atualizados++;
                                }
                                else
                                {
                                    string insertSql = @"INSERT INTO Computadores (MAC, IP, ColaboradorCPF, Hostname, Fabricante, Processador, ProcessadorFabricante, ProcessadorCore, ProcessadorThread, ProcessadorClock, Ram, RamTipo, RamVelocidade, RamVoltagem, RamPorModule, ArmazenamentoC, ArmazenamentoCTotal, ArmazenamentoCLivre, ArmazenamentoD, ArmazenamentoDTotal, ArmazenamentoDLivre, ConsumoCPU, SO, DataColeta, PartNumber)
                                                       VALUES (@MAC, @IP, @ColaboradorCPF, @Hostname, @Fabricante, @Processador, @ProcessadorFabricante, @ProcessadorCore, @ProcessadorThread, @ProcessadorClock, @Ram, @RamTipo, @RamVelocidade, @RamVoltagem, @RamPorModule, @ArmazenamentoC, @ArmazenamentoCTotal, @ArmazenamentoCLivre, @ArmazenamentoD, @ArmazenamentoDTotal, @ArmazenamentoDLivre, @ConsumoCPU, @SO, @DataColeta, @PartNumber)";
                                    using (var cmd = connection.CreateCommand())
                                    {
                                        cmd.Transaction = transaction;
                                        cmd.CommandText = insertSql;
                                        AddComputadorParameters(cmd, computador);
                                        var pDate = cmd.CreateParameter(); pDate.ParameterName = "@DataColeta"; pDate.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); cmd.Parameters.Add(pDate);
                                        cmd.ExecuteNonQuery();
                                    }
                                    adicionados++;
                                }
                            }
                            transaction.Commit();
                            TempData["SuccessMessage"] = $"{adicionados} computadores adicionados e {atualizados} atualizados com sucesso.";
                            if (invalidCpfs.Any())
                            {
                                TempData["WarningMessage"] = $"Os seguintes computadores (MAC) foram importados, mas o CPF do colaborador não foi encontrado: {string.Join(", ", invalidCpfs)}";
                            }
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            _logger.LogError(ex, "Erro ao salvar os dados do Excel. A transação foi revertida.");
                            TempData["ErrorMessage"] = "Ocorreu um erro ao salvar os dados. Nenhuma alteração foi feita.";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao importar o arquivo Excel.");
                TempData["ErrorMessage"] = "Ocorreu um erro durante a importação do arquivo. Verifique se o formato está correto.";
            }

            return RedirectToAction(nameof(Index));
        }

        private void AddComputadorParameters(IDbCommand cmd, Computador computador)
        {
            var p1 = cmd.CreateParameter(); p1.ParameterName = "@MAC"; p1.Value = computador.MAC; cmd.Parameters.Add(p1);
            var p2 = cmd.CreateParameter(); p2.ParameterName = "@IP"; p2.Value = (object)computador.IP ?? DBNull.Value; cmd.Parameters.Add(p2);
            var p3 = cmd.CreateParameter(); p3.ParameterName = "@ColaboradorCPF"; p3.Value = (object)computador.ColaboradorCPF ?? DBNull.Value; cmd.Parameters.Add(p3);
            var p4 = cmd.CreateParameter(); p4.ParameterName = "@Hostname"; p4.Value = (object)computador.Hostname ?? DBNull.Value; cmd.Parameters.Add(p4);
            var p5 = cmd.CreateParameter(); p5.ParameterName = "@Fabricante"; p5.Value = (object)computador.Fabricante ?? DBNull.Value; cmd.Parameters.Add(p5);
            var p6 = cmd.CreateParameter(); p6.ParameterName = "@Processador"; p6.Value = (object)computador.Processador ?? DBNull.Value; cmd.Parameters.Add(p6);
            var p7 = cmd.CreateParameter(); p7.ParameterName = "@ProcessadorFabricante"; p7.Value = (object)computador.ProcessadorFabricante ?? DBNull.Value; cmd.Parameters.Add(p7);
            var p8 = cmd.CreateParameter(); p8.ParameterName = "@ProcessadorCore"; p8.Value = (object)computador.ProcessadorCore ?? DBNull.Value; cmd.Parameters.Add(p8);
            var p9 = cmd.CreateParameter(); p9.ParameterName = "@ProcessadorThread"; p9.Value = (object)computador.ProcessadorThread ?? DBNull.Value; cmd.Parameters.Add(p9);
            var p10 = cmd.CreateParameter(); p10.ParameterName = "@ProcessadorClock"; p10.Value = (object)computador.ProcessadorClock ?? DBNull.Value; cmd.Parameters.Add(p10);
            var p11 = cmd.CreateParameter(); p11.ParameterName = "@Ram"; p11.Value = (object)computador.Ram ?? DBNull.Value; cmd.Parameters.Add(p11);
            var p12 = cmd.CreateParameter(); p12.ParameterName = "@RamTipo"; p12.Value = (object)computador.RamTipo ?? DBNull.Value; cmd.Parameters.Add(p12);
            var p13 = cmd.CreateParameter(); p13.ParameterName = "@RamVelocidade"; p13.Value = (object)computador.RamVelocidade ?? DBNull.Value; cmd.Parameters.Add(p13);
            var p14 = cmd.CreateParameter(); p14.ParameterName = "@RamVoltagem"; p14.Value = (object)computador.RamVoltagem ?? DBNull.Value; cmd.Parameters.Add(p14);
            var p15 = cmd.CreateParameter(); p15.ParameterName = "@RamPorModule"; p15.Value = (object)computador.RamPorModule ?? DBNull.Value; cmd.Parameters.Add(p15);
            var p16 = cmd.CreateParameter(); p16.ParameterName = "@ArmazenamentoC"; p16.Value = (object)computador.ArmazenamentoC ?? DBNull.Value; cmd.Parameters.Add(p16);
            var p17 = cmd.CreateParameter(); p17.ParameterName = "@ArmazenamentoCTotal"; p17.Value = (object)computador.ArmazenamentoCTotal ?? DBNull.Value; cmd.Parameters.Add(p17);
            var p18 = cmd.CreateParameter(); p18.ParameterName = "@ArmazenamentoCLivre"; p18.Value = (object)computador.ArmazenamentoCLivre ?? DBNull.Value; cmd.Parameters.Add(p18);
            var p19 = cmd.CreateParameter(); p19.ParameterName = "@ArmazenamentoD"; p19.Value = (object)computador.ArmazenamentoD ?? DBNull.Value; cmd.Parameters.Add(p19);
            var p20 = cmd.CreateParameter(); p20.ParameterName = "@ArmazenamentoDTotal"; p20.Value = (object)computador.ArmazenamentoDTotal ?? DBNull.Value; cmd.Parameters.Add(p20);
            var p21 = cmd.CreateParameter(); p21.ParameterName = "@ArmazenamentoDLivre"; p21.Value = (object)computador.ArmazenamentoDLivre ?? DBNull.Value; cmd.Parameters.Add(p21);
            var p22 = cmd.CreateParameter(); p22.ParameterName = "@ConsumoCPU"; p22.Value = (object)computador.ConsumoCPU ?? DBNull.Value; cmd.Parameters.Add(p22);
            var p23 = cmd.CreateParameter(); p23.ParameterName = "@SO"; p23.Value = (object)computador.SO ?? DBNull.Value; cmd.Parameters.Add(p23);
            var p24 = cmd.CreateParameter(); p24.ParameterName = "@PartNumber"; p24.Value = (object)computador.PartNumber ?? DBNull.Value; cmd.Parameters.Add(p24);
        }

        private Computador FindComputadorById(string id, IDbConnection connection, IDbTransaction transaction)
        {
            Computador computador = null;

            try
            {
                string sql = "SELECT * FROM Computadores WHERE MAC = @MAC";
                using (var cmd = connection.CreateCommand())
                {
                    cmd.Transaction = transaction;
                    cmd.CommandText = sql;
                    var p1 = cmd.CreateParameter(); p1.ParameterName = "@MAC"; p1.Value = id; cmd.Parameters.Add(p1);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            computador = new Computador
                            {
                                MAC = reader["MAC"].ToString(),
                                IP = reader["IP"].ToString(),
                                ColaboradorCPF = reader["ColaboradorCPF"] != DBNull.Value ? reader["ColaboradorCPF"].ToString() : null,
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
                                DataColeta = reader["DataColeta"] != DBNull.Value ? Convert.ToDateTime(reader["DataColeta"]) : (DateTime?)null,
                                PartNumber = reader["PartNumber"].ToString()
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao encontrar computador por ID.");
                if (transaction != null) throw;
            }

            return computador;
        }

        private List<string> GetDistinctComputerValues(IDbConnection connection, string columnName)
        {
            var values = new List<string>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = $"SELECT DISTINCT {columnName} FROM Computadores WHERE {columnName} IS NOT NULL ORDER BY {columnName}";
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
        public async Task<IActionResult> Create(ComputadorViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    using (var connection = _databaseService.CreateConnection())
                    {
                        connection.Open();

                        string sql = "INSERT INTO Computadores (MAC, IP, ColaboradorCPF, Hostname, Fabricante, Processador, ProcessadorFabricante, ProcessadorCore, ProcessadorThread, ProcessadorClock, Ram, RamTipo, RamVelocidade, RamVoltagem, RamPorModule, ArmazenamentoC, ArmazenamentoCTotal, ArmazenamentoCLivre, ArmazenamentoD, ArmazenamentoDTotal, ArmazenamentoDLivre, ConsumoCPU, SO, DataColeta, PartNumber) VALUES (@MAC, @IP, @ColaboradorCPF, @Hostname, @Fabricante, @Processador, @ProcessadorFabricante, @ProcessadorCore, @ProcessadorThread, @ProcessadorClock, @Ram, @RamTipo, @RamVelocidade, @RamVoltagem, @RamPorModule, @ArmazenamentoC, @ArmazenamentoCTotal, @ArmazenamentoCLivre, @ArmazenamentoD, @ArmazenamentoDTotal, @ArmazenamentoDLivre, @ConsumoCPU, @SO, @DataColeta, @PartNumber)";

                        using (var cmd = connection.CreateCommand())
                        {
                            cmd.CommandText = sql;
                            var comp = new Computador
                            {
                                MAC = viewModel.MAC,
                                IP = viewModel.IP,
                                ColaboradorCPF = viewModel.ColaboradorCPF,
                                Hostname = viewModel.Hostname,
                                Fabricante = viewModel.Fabricante,
                                Processador = viewModel.Processador,
                                ProcessadorFabricante = viewModel.ProcessadorFabricante,
                                ProcessadorCore = viewModel.ProcessadorCore,
                                ProcessadorThread = viewModel.ProcessadorThread,
                                ProcessadorClock = viewModel.ProcessadorClock,
                                Ram = viewModel.Ram,
                                RamTipo = viewModel.RamTipo,
                                RamVelocidade = viewModel.RamVelocidade,
                                RamVoltagem = viewModel.RamVoltagem,
                                RamPorModule = viewModel.RamPorModule,
                                ArmazenamentoC = viewModel.ArmazenamentoC,
                                ArmazenamentoCTotal = viewModel.ArmazenamentoCTotal,
                                ArmazenamentoCLivre = viewModel.ArmazenamentoCLivre,
                                ArmazenamentoD = viewModel.ArmazenamentoD,
                                ArmazenamentoDTotal = viewModel.ArmazenamentoDTotal,
                                ArmazenamentoDLivre = viewModel.ArmazenamentoDLivre,
                                ConsumoCPU = viewModel.ConsumoCPU,
                                SO = viewModel.SO,
                                PartNumber = viewModel.PartNumber
                            };
                            AddComputadorParameters(cmd, comp);
                            var pDate = cmd.CreateParameter(); pDate.ParameterName = "@DataColeta"; pDate.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); cmd.Parameters.Add(pDate);

                            cmd.ExecuteNonQuery();
                        }
                    }

                    await _persistentLogService.LogChangeAsync("Computador", "Create", User.Identity.Name, null, viewModel);

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
                SO = computador.SO,
                PartNumber = computador.PartNumber
            };
            ViewData["Colaboradores"] = new SelectList(GetColaboradores(), "CPF", "Nome", viewModel.ColaboradorCPF);
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string id, ComputadorViewModel viewModel)
        {
            if (id != viewModel.MAC) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var oldComputador = FindComputadorById(id);

                    using (var connection = _databaseService.CreateConnection())
                    {
                        connection.Open();
                        string sql = "UPDATE Computadores SET IP = @IP, ColaboradorCPF = @ColaboradorCPF, Hostname = @Hostname, Fabricante = @Fabricante, Processador = @Processador, ProcessadorFabricante = @ProcessadorFabricante, ProcessadorCore = @ProcessadorCore, ProcessadorThread = @ProcessadorThread, ProcessadorClock = @ProcessadorClock, Ram = @Ram, RamTipo = @RamTipo, RamVelocidade = @RamVelocidade, RamVoltagem = @RamVoltagem, RamPorModule = @RamPorModule, ArmazenamentoC = @ArmazenamentoC, ArmazenamentoCTotal = @ArmazenamentoCTotal, ArmazenamentoCLivre = @ArmazenamentoCLivre, ArmazenamentoD = @ArmazenamentoD, ArmazenamentoDTotal = @ArmazenamentoDTotal, ArmazenamentoDLivre = @ArmazenamentoDLivre, ConsumoCPU = @ConsumoCPU, SO = @SO, PartNumber = @PartNumber WHERE MAC = @MAC";

                        using (var cmd = connection.CreateCommand())
                        {
                            cmd.CommandText = sql;
                            var comp = new Computador
                            {
                                MAC = viewModel.MAC,
                                IP = viewModel.IP,
                                ColaboradorCPF = viewModel.ColaboradorCPF,
                                Hostname = viewModel.Hostname,
                                Fabricante = viewModel.Fabricante,
                                Processador = viewModel.Processador,
                                ProcessadorFabricante = viewModel.ProcessadorFabricante,
                                ProcessadorCore = viewModel.ProcessadorCore,
                                ProcessadorThread = viewModel.ProcessadorThread,
                                ProcessadorClock = viewModel.ProcessadorClock,
                                Ram = viewModel.Ram,
                                RamTipo = viewModel.RamTipo,
                                RamVelocidade = viewModel.RamVelocidade,
                                RamVoltagem = viewModel.RamVoltagem,
                                RamPorModule = viewModel.RamPorModule,
                                ArmazenamentoC = viewModel.ArmazenamentoC,
                                ArmazenamentoCTotal = viewModel.ArmazenamentoCTotal,
                                ArmazenamentoCLivre = viewModel.ArmazenamentoCLivre,
                                ArmazenamentoD = viewModel.ArmazenamentoD,
                                ArmazenamentoDTotal = viewModel.ArmazenamentoDTotal,
                                ArmazenamentoDLivre = viewModel.ArmazenamentoDLivre,
                                ConsumoCPU = viewModel.ConsumoCPU,
                                SO = viewModel.SO,
                                PartNumber = viewModel.PartNumber
                            };
                            AddComputadorParameters(cmd, comp);

                            cmd.ExecuteNonQuery();
                        }
                    }

                    await _persistentLogService.LogChangeAsync("Computador", "Update", User.Identity.Name, oldComputador, viewModel);

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
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            try
            {
                var computador = FindComputadorById(id);
                if (computador != null)
                {
                    using (var connection = _databaseService.CreateConnection())
                    {
                        connection.Open();
                        string sql = "DELETE FROM Computadores WHERE MAC = @MAC";
                        using (var cmd = connection.CreateCommand())
                        {
                            cmd.CommandText = sql;
                            var p1 = cmd.CreateParameter(); p1.ParameterName = "@MAC"; p1.Value = id; cmd.Parameters.Add(p1);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    await _persistentLogService.LogChangeAsync("Computador", "Delete", User.Identity.Name, computador, null);
                }
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
                using (var connection = _databaseService.CreateConnection())
                {
                    connection.Open();
                    string sql = "SELECT comp.*, col.Nome AS ColaboradorNome FROM Computadores comp LEFT JOIN Colaboradores col ON comp.ColaboradorCPF = col.CPF WHERE comp.MAC = @MAC";
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = sql;
                        var p1 = cmd.CreateParameter(); p1.ParameterName = "@MAC"; p1.Value = id; cmd.Parameters.Add(p1);
                        using (var reader = cmd.ExecuteReader())
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
                                DataColeta = reader["DataColeta"] != DBNull.Value ? Convert.ToDateTime(reader["DataColeta"]) : (DateTime?)null,
                                PartNumber = reader["PartNumber"].ToString()
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

        private string SanitizeCpf(string cpf)
        {
            if (string.IsNullOrEmpty(cpf)) return cpf;
            return new string(cpf.Where(char.IsDigit).ToArray());
        }
    }
}