using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Web.Models;
using Web.Services;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;

namespace Web.Controllers
{
    [Authorize]
    public class ColaboradoresController : Controller
    {
        private readonly IDatabaseService _databaseService;
        private readonly ILogger<ColaboradoresController> _logger;
        private readonly PersistentLogService _persistentLogService;

        public ColaboradoresController(IDatabaseService databaseService, ILogger<ColaboradoresController> logger, PersistentLogService persistentLogService)
        {
            _databaseService = databaseService;
            _logger = logger;
            _persistentLogService = persistentLogService;
        }

        private string SanitizeCpf(string cpf)
        {
            if (string.IsNullOrEmpty(cpf)) return cpf;
            return new string(cpf.Where(char.IsDigit).ToArray());
        }

        public IActionResult Index(
            string sortOrder, string searchString,
            List<string> currentFiliais, List<string> currentSetores, List<string> currentSmartphones,
            List<string> currentTelefoneFixos, List<string> currentRamais, List<string> currentCoordenadores,
            int pageNumber = 1, int pageSize = 25)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NomeSortParm"] = string.IsNullOrEmpty(sortOrder) ? "nome_desc" : "";
            ViewData["EmailSortParm"] = sortOrder == "email" ? "email_desc" : "email";
            ViewData["FilialSortParm"] = sortOrder == "filial" ? "filial_desc" : "filial";
            ViewData["SetorSortParm"] = sortOrder == "setor" ? "setor_desc" : "setor";

            var viewModel = new ColaboradorIndexViewModel
            {
                Colaboradores = new List<Colaborador>(),
                PageNumber = pageNumber,
                PageSize = pageSize,
                SearchString = searchString,
                CurrentSort = sortOrder,
                CurrentFiliais = currentFiliais ?? new List<string>(),
                CurrentSetores = currentSetores ?? new List<string>(),
                CurrentSmartphones = currentSmartphones ?? new List<string>(),
                CurrentTelefoneFixos = currentTelefoneFixos ?? new List<string>(),
                CurrentRamais = currentRamais ?? new List<string>(),
                CurrentCoordenadores = currentCoordenadores ?? new List<string>()
            };

            try
            {
                using (var connection = _databaseService.CreateConnection())
                {
                    connection.Open();

                    viewModel.Filiais = GetDistinctColaboradorValues(connection, "Filial");
                    viewModel.Setores = GetDistinctColaboradorValues(connection, "Setor");
                    viewModel.Smartphones = GetDistinctColaboradorValues(connection, "Smartphone");
                    viewModel.TelefoneFixos = GetDistinctColaboradorValues(connection, "TelefoneFixo");
                    viewModel.Ramais = GetDistinctColaboradorValues(connection, "Ramal");
                    viewModel.Coordenadores = GetCoordenadores().Select(c => c.Nome).ToList();

                    var whereClauses = new List<string>();
                    var parameters = new Dictionary<string, object>();
                    string baseSql = "FROM Colaboradores c LEFT JOIN Colaboradores co ON c.CoordenadorCPF = co.CPF";

                    if (!string.IsNullOrEmpty(searchString))
                    {
                        whereClauses.Add("(c.Nome LIKE @search OR c.CPF LIKE @search OR c.Email LIKE @search)");
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

                    Action<string, List<string>> addPhoneInClause = (tipo, values) =>
                    {
                        if (values != null && values.Any())
                        {
                            var paramNames = new List<string>();
                            for (int i = 0; i < values.Count; i++)
                            {
                                var paramName = $"@{tipo.ToLower()}{i}";
                                paramNames.Add(paramName);
                                parameters.Add(paramName, values[i]);
                            }
                            whereClauses.Add($"c.CPF IN (SELECT ColaboradorCPF FROM ColaboradorTelefones WHERE Tipo = '{tipo}' AND Numero IN ({string.Join(", ", paramNames)}))");
                        }
                    };

                    addInClause("c.Filial", viewModel.CurrentFiliais);
                    addInClause("c.Setor", viewModel.CurrentSetores);
                    addPhoneInClause("Smartphone", viewModel.CurrentSmartphones);
                    addPhoneInClause("TelefoneFixo", viewModel.CurrentTelefoneFixos);
                    addPhoneInClause("Ramal", viewModel.CurrentRamais);
                    addInClause("co.Nome", viewModel.CurrentCoordenadores);

                    // Add role-based filtering
                    if (!User.IsInRole("Admin") && !User.IsInRole("Diretoria/RH"))
                    {
                        var userCpf = User.Claims.FirstOrDefault(c => c.Type == "ColaboradorCPF")?.Value;
                        if (!string.IsNullOrEmpty(userCpf))
                        {
                            whereClauses.Add("(c.CPF = @userCpf OR c.CoordenadorCPF = @userCpf)");
                            parameters.Add("@userCpf", userCpf);
                        }
                        else
                        {
                            // If the user doesn't have a CPF claim (e.g. they aren't linked to a Colaborador), they should see nothing
                            whereClauses.Add("1 = 0");
                        }
                    }

                    string whereSql = whereClauses.Any() ? $"WHERE {string.Join(" AND ", whereClauses)}" : "";

                    string countSql = $"SELECT COUNT(c.CPF) {baseSql} {whereSql}";
                    using (var countCommand = connection.CreateCommand())
                    {
                        countCommand.CommandText = countSql;
                        foreach (var p in parameters) {
                             var param = countCommand.CreateParameter();
                             param.ParameterName = p.Key;
                             param.Value = p.Value;
                             countCommand.Parameters.Add(param);
                        }
                        var result = countCommand.ExecuteScalar();
                        viewModel.TotalCount = result != DBNull.Value ? Convert.ToInt32(result) : 0;
                    }

                    string orderBySql;
                    switch (sortOrder)
                    {
                        case "nome_desc": orderBySql = "ORDER BY c.Nome DESC"; break;
                        case "email": orderBySql = "ORDER BY c.Email"; break;
                        case "email_desc": orderBySql = "ORDER BY c.Email DESC"; break;
                        case "filial": orderBySql = "ORDER BY c.Filial"; break;
                        case "filial_desc": orderBySql = "ORDER BY c.Filial DESC"; break;
                        case "setor": orderBySql = "ORDER BY c.Setor"; break;
                        case "setor_desc": orderBySql = "ORDER BY c.Setor DESC"; break;
                        default: orderBySql = "ORDER BY c.Nome"; break;
                    }

                    string sql = $"SELECT c.*, co.Nome as CoordenadorNome {baseSql} {whereSql} {orderBySql} LIMIT @pageSize OFFSET @offset";
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
                                viewModel.Colaboradores.Add(new Colaborador
                                {
                                    CPF = reader["CPF"].ToString(),
                                    Nome = reader["Nome"].ToString(),
                                    Email = reader["Email"].ToString(),
                                    Filial = reader["Filial"].ToString(),
                                    Setor = reader["Setor"].ToString(),
                                    DataInclusao = reader["DataInclusao"] != DBNull.Value ? Convert.ToDateTime(reader["DataInclusao"]) : (DateTime?)null,
                                    DataAlteracao = reader["DataAlteracao"] != DBNull.Value ? Convert.ToDateTime(reader["DataAlteracao"]) : (DateTime?)null,
                                    CoordenadorNome = reader["CoordenadorNome"] != DBNull.Value ? reader["CoordenadorNome"].ToString() : null
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter a lista de colaboradores.");
                ViewBag.Message = "Ocorreu um erro ao obter a lista de colaboradores. Por favor, tente novamente mais tarde.";
            }

            return View(viewModel);
        }

        // GET: Colaboradores/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewBag.Coordenadores = new SelectList(GetCoordenadores(), "CPF", "Nome");
            return View();
        }

        // POST: Colaboradores/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Colaborador colaborador)
        {
            colaborador.CPF = SanitizeCpf(colaborador.CPF);
            colaborador.CoordenadorCPF = SanitizeCpf(colaborador.CoordenadorCPF);

            if (ModelState.IsValid)
            {
                try
                {
                    using (var connection = _databaseService.CreateConnection())
                    {
                        connection.Open();
                        string sql = @"INSERT INTO Colaboradores (CPF, Nome, Email, Filial, Setor, Alarme, Videoporteiro, Obs, DataInclusao, CoordenadorCPF) 
                                       VALUES (@CPF, @Nome, @Email, @Filial, @Setor, @Alarme, @Videoporteiro, @Obs, @DataInclusao, @CoordenadorCPF)";
                        using (var transaction = connection.BeginTransaction())
                        {
                            try
                            {
                                using (var cmd = connection.CreateCommand())
                                {
                                    cmd.Transaction = transaction;
                                    cmd.CommandText = sql;
                                    AddColaboradorParameters(cmd, colaborador);
                                    var pDate = cmd.CreateParameter(); pDate.ParameterName = "@DataInclusao"; pDate.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); cmd.Parameters.Add(pDate);
                                    cmd.ExecuteNonQuery();
                                }

                                SalvarCredenciaisETelefones(connection, transaction, colaborador);
                                transaction.Commit();
                            }
                            catch
                            {
                                transaction.Rollback();
                                throw;
                            }
                        }
                    }

                    await _persistentLogService.LogChangeAsync("Colaborador", "Create", User.Identity.Name, null, colaborador);

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao criar colaborador.");
                    ModelState.AddModelError(string.Empty, "Ocorreu um erro ao criar o colaborador. Verifique se o CPF já existe.");
                }
            }
            ViewBag.Coordenadores = new SelectList(GetCoordenadores(), "CPF", "Nome", colaborador.CoordenadorCPF);
            return View(colaborador);
        }

        // GET: Colaboradores/Details/5
        public IActionResult Details(string id)
        {
            if (id == null) return NotFound();
            var sanitizedId = SanitizeCpf(id);
            Colaborador colaborador = FindColaboradorById(sanitizedId);
            if (colaborador == null) return NotFound();

            // Authorization check
            if (!User.IsInRole("Admin") && !User.IsInRole("Diretoria/RH"))
            {
                var userCpf = User.Claims.FirstOrDefault(c => c.Type == "ColaboradorCPF")?.Value;
                // If the current user's CPF doesn't match the accessed CPF and the accessed user's Coordinator CPF
                if (userCpf != colaborador.CPF && userCpf != colaborador.CoordenadorCPF)
                {
                    return Forbid();
                }
            }

            return View(colaborador);
        }

        // GET: Colaboradores/Edit/5
        [Authorize(Roles = "Admin")]
        public IActionResult Edit(string id)
        {
            if (id == null) return NotFound();
            Colaborador colaborador = FindColaboradorById(SanitizeCpf(id));
            if (colaborador == null) return NotFound();
            ViewBag.Coordenadores = new SelectList(GetCoordenadores(), "CPF", "Nome", colaborador.CoordenadorCPF);
            return View(colaborador);
        }

        // POST: Colaboradores/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string id, Colaborador colaborador)
        {
            var sanitizedId = SanitizeCpf(id);
            var cleanColaboradorCpf = SanitizeCpf(colaborador.CPF);
            colaborador.CoordenadorCPF = SanitizeCpf(colaborador.CoordenadorCPF);

            if (sanitizedId != cleanColaboradorCpf && id != colaborador.CPF) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var oldColaborador = FindColaboradorById(sanitizedId);
                    if (oldColaborador == null) return NotFound();

                    using (var connection = _databaseService.CreateConnection())
                    {
                        connection.Open();
                        string sql = @"UPDATE Colaboradores SET 
                                       Nome = @Nome, Email = @Email, Filial = @Filial, Setor = @Setor, Alarme = @Alarme, Videoporteiro = @Videoporteiro,
                                       Obs = @Obs, DataAlteracao = @DataAlteracao, CoordenadorCPF = @CoordenadorCPF
                                       WHERE CPF = @OldCPF OR REPLACE(REPLACE(REPLACE(CPF, '.', ''), '-', ''), ' ', '') = @CleanCPF";
                        using (var transaction = connection.BeginTransaction())
                        {
                            try
                            {
                                using (var cmd = connection.CreateCommand())
                                {
                                    cmd.Transaction = transaction;
                                    cmd.CommandText = sql;
                                    AddColaboradorParameters(cmd, colaborador);
                                    var pOld = cmd.CreateParameter(); pOld.ParameterName = "@OldCPF"; pOld.Value = oldColaborador.CPF; cmd.Parameters.Add(pOld);
                                    var pClean = cmd.CreateParameter(); pClean.ParameterName = "@CleanCPF"; pClean.Value = sanitizedId; cmd.Parameters.Add(pClean);
                                    var pDate = cmd.CreateParameter(); pDate.ParameterName = "@DataAlteracao"; pDate.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); cmd.Parameters.Add(pDate);
                                    cmd.ExecuteNonQuery();
                                }

                                SalvarCredenciaisETelefones(connection, transaction, colaborador);
                                transaction.Commit();
                            }
                            catch
                            {
                                transaction.Rollback();
                                throw;
                            }
                        }
                    }

                    await _persistentLogService.LogChangeAsync("Colaborador", "Update", User.Identity.Name, oldColaborador, colaborador);

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao editar colaborador.");
                    ModelState.AddModelError(string.Empty, "Ocorreu um erro ao editar o colaborador.");
                }
            }
            ViewBag.Coordenadores = new SelectList(GetCoordenadores(), "CPF", "Nome", colaborador.CoordenadorCPF);
            return View(colaborador);
        }

        [HttpGet]
        public IActionResult GerarTermoPdf(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var colaborador = FindColaboradorById(SanitizeCpf(id));
            if (colaborador == null)
            {
                return NotFound();
            }

            var computadores = new List<Computador>();
            var monitores = new List<Web.Models.Monitor>();
            var perifericos = new List<Periferico>();
            var smartphones = new List<Smartphone>();

            using (var connection = _databaseService.CreateConnection())
            {
                connection.Open();

                // Computadores
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT c.Hostname, c.Fabricante, COALESCE(p.Nome, c.Processador) AS Processador, c.Ram, c.SO, c.MAC FROM Computadores c LEFT JOIN Processadores p ON c.ProcessadorId = p.Id WHERE c.ColaboradorCPF = @CPF";
                    var p = cmd.CreateParameter();
                    p.ParameterName = "@CPF";
                    p.Value = colaborador.CPF;
                    cmd.Parameters.Add(p);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            computadores.Add(new Computador
                            {
                                Hostname = reader["Hostname"].ToString(),
                                Fabricante = reader["Fabricante"].ToString(),
                                Processador = reader["Processador"].ToString(),
                                Ram = reader["Ram"].ToString(),
                                SO = reader["SO"].ToString(),
                                MAC = reader["MAC"].ToString()
                            });
                        }
                    }
                }

                // Monitores
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT Marca, Modelo, Tamanho, PartNumber FROM Monitores WHERE ColaboradorCPF = @CPF";
                    var p = cmd.CreateParameter();
                    p.ParameterName = "@CPF";
                    p.Value = colaborador.CPF;
                    cmd.Parameters.Add(p);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            monitores.Add(new Web.Models.Monitor
                            {
                                Marca = reader["Marca"].ToString(),
                                Modelo = reader["Modelo"].ToString(),
                                Tamanho = reader["Tamanho"].ToString(),
                                PartNumber = reader["PartNumber"].ToString()
                            });
                        }
                    }
                }

                // Perifericos
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT Tipo, PartNumber FROM Perifericos WHERE ColaboradorCPF = @CPF";
                    var p = cmd.CreateParameter();
                    p.ParameterName = "@CPF";
                    p.Value = colaborador.CPF;
                    cmd.Parameters.Add(p);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            perifericos.Add(new Periferico
                            {
                                Tipo = reader["Tipo"].ToString(),
                                PartNumber = reader["PartNumber"].ToString()
                            });
                        }
                    }
                }

                // Smartphones
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT Modelo, IMEI1, IMEI2, MAC FROM Smartphones WHERE Usuario LIKE @Nome";
                    var p = cmd.CreateParameter();
                    p.ParameterName = "@Nome";
                    p.Value = "%" + colaborador.Nome + "%";
                    cmd.Parameters.Add(p);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            smartphones.Add(new Smartphone
                            {
                                Modelo = reader["Modelo"].ToString(),
                                IMEI1 = reader["IMEI1"].ToString(),
                                IMEI2 = reader["IMEI2"] != DBNull.Value ? reader["IMEI2"].ToString() : "",
                                MAC = reader["MAC"] != DBNull.Value ? reader["MAC"].ToString() : ""
                            });
                        }
                    }
                }
            }

            using (var memoryStream = new MemoryStream())
            {
                using (var writer = new PdfWriter(memoryStream))
                {
                    using (var pdf = new PdfDocument(writer))
                    {
                        var document = new Document(pdf);
                        document.SetTextAlignment(TextAlignment.LEFT);

                        // Titulo
                        var titulo = new Paragraph("Termo de Responsabilidade")
                            .SetTextAlignment(TextAlignment.CENTER)
                            .SetFontSize(16)
                            .SetMarginBottom(20);
                        titulo.SetProperty(iText.Layout.Properties.Property.BOLD_SIMULATION, true);
                        document.Add(titulo);

                        // Dados do Colaborador
                        document.Add(new Paragraph($"Eu, {colaborador.Nome}, CPF {colaborador.CPF},")
                            .SetMarginBottom(10));
                        document.Add(new Paragraph("Declaro ter recebido os equipamentos abaixo listados em perfeitas condições de uso, " +
                            "comprometendo-me a zelar por sua conservação e a utilizá-los exclusivamente para o desempenho das minhas " +
                            "funções profissionais. Estou ciente de que deverei restituí-los imediatamente em caso de desligamento ou " +
                            "quando solicitado pela empresa.")
                            .SetMarginBottom(20));

                        // Equipamentos
                        if (computadores.Any())
                        {
                            var pComputadores = new Paragraph("Computadores:");
                            pComputadores.SetProperty(iText.Layout.Properties.Property.BOLD_SIMULATION, true);
                            document.Add(pComputadores);
                            var table = new Table(new float[] { 3, 2, 2, 2, 3 });
                            table.SetWidth(UnitValue.CreatePercentValue(100));
                            table.AddHeaderCell("Hostname");
                            table.AddHeaderCell("Fabricante");
                            table.AddHeaderCell("Processador");
                            table.AddHeaderCell("RAM");
                            table.AddHeaderCell("MAC");
                            foreach (var comp in computadores)
                            {
                                table.AddCell(comp.Hostname ?? "");
                                table.AddCell(comp.Fabricante ?? "");
                                table.AddCell(comp.Processador ?? "");
                                table.AddCell(comp.Ram ?? "");
                                table.AddCell(comp.MAC ?? "");
                            }
                            document.Add(table);
                            document.Add(new Paragraph("\n"));
                        }

                        if (monitores.Any())
                        {
                            var pMonitores = new Paragraph("Monitores:");
                            pMonitores.SetProperty(iText.Layout.Properties.Property.BOLD_SIMULATION, true);
                            document.Add(pMonitores);
                            var table = new Table(new float[] { 2, 3, 2, 3 });
                            table.SetWidth(UnitValue.CreatePercentValue(100));
                            table.AddHeaderCell("Marca");
                            table.AddHeaderCell("Modelo");
                            table.AddHeaderCell("Tamanho");
                            table.AddHeaderCell("PartNumber");
                            foreach (var mon in monitores)
                            {
                                table.AddCell(mon.Marca ?? "");
                                table.AddCell(mon.Modelo ?? "");
                                table.AddCell(mon.Tamanho ?? "");
                                table.AddCell(mon.PartNumber ?? "");
                            }
                            document.Add(table);
                            document.Add(new Paragraph("\n"));
                        }

                        if (perifericos.Any())
                        {
                            var pPerifericos = new Paragraph("Periféricos:");
                            pPerifericos.SetProperty(iText.Layout.Properties.Property.BOLD_SIMULATION, true);
                            document.Add(pPerifericos);
                            var table = new Table(new float[] { 1, 1 });
                            table.SetWidth(UnitValue.CreatePercentValue(100));
                            table.AddHeaderCell("Tipo");
                            table.AddHeaderCell("PartNumber");
                            foreach (var per in perifericos)
                            {
                                table.AddCell(per.Tipo ?? "");
                                table.AddCell(per.PartNumber ?? "");
                            }
                            document.Add(table);
                            document.Add(new Paragraph("\n"));
                        }

                        if (smartphones.Any() || !string.IsNullOrEmpty(colaborador.Smartphone))
                        {
                            var pSmartphones = new Paragraph("Smartphones:");
                            pSmartphones.SetProperty(iText.Layout.Properties.Property.BOLD_SIMULATION, true);
                            document.Add(pSmartphones);
                            if (smartphones.Any())
                            {
                                var table = new Table(new float[] { 2, 3, 3, 2 });
                                table.SetWidth(UnitValue.CreatePercentValue(100));
                                table.AddHeaderCell("Modelo");
                                table.AddHeaderCell("IMEI 1");
                                table.AddHeaderCell("IMEI 2");
                                table.AddHeaderCell("MAC");
                                foreach (var smp in smartphones)
                                {
                                    table.AddCell(smp.Modelo ?? "");
                                    table.AddCell(smp.IMEI1 ?? "");
                                    table.AddCell(smp.IMEI2 ?? "");
                                    table.AddCell(smp.MAC ?? "");
                                }
                                document.Add(table);
                            }
                            else
                            {
                                document.Add(new Paragraph($"Smartphone registrado: {colaborador.Smartphone}"));
                            }
                            document.Add(new Paragraph("\n"));
                        }

                        // Assinatura
                        document.Add(new Paragraph("\n\n\n"));
                        document.Add(new Paragraph("____________________________________________________")
                            .SetTextAlignment(TextAlignment.CENTER));
                        document.Add(new Paragraph(colaborador.Nome)
                            .SetTextAlignment(TextAlignment.CENTER));
                        document.Add(new Paragraph(DateTime.Now.ToString("dd/MM/yyyy"))
                            .SetTextAlignment(TextAlignment.CENTER));
                    }
                }

                return File(memoryStream.ToArray(), "application/pdf", $"Termo_Responsabilidade_{colaborador.Nome.Replace(" ", "_")}.pdf");
            }
        }

        // GET: Colaboradores/Delete/5
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(string id)
        {
            if (id == null) return NotFound();
            Colaborador colaborador = FindColaboradorById(SanitizeCpf(id));
            if (colaborador == null) return NotFound();
            return View(colaborador);
        }

        // POST: Colaboradores/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var sanitizedId = SanitizeCpf(id);
            try
            {
                var colaborador = FindColaboradorById(sanitizedId);
                if (colaborador != null)
                {
                    using (var connection = _databaseService.CreateConnection())
                    {
                        connection.Open();
                        string sql = "DELETE FROM Colaboradores WHERE CPF = @CPF OR REPLACE(REPLACE(REPLACE(CPF, '.', ''), '-', ''), ' ', '') = @CleanCPF";
                        using (var cmd = connection.CreateCommand())
                        {
                            cmd.CommandText = sql;
                            var p1 = cmd.CreateParameter(); p1.ParameterName = "@CPF"; p1.Value = colaborador.CPF; cmd.Parameters.Add(p1);
                            var p2 = cmd.CreateParameter(); p2.ParameterName = "@CleanCPF"; p2.Value = sanitizedId; cmd.Parameters.Add(p2);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    await _persistentLogService.LogChangeAsync("Colaborador", "Delete", User.Identity.Name, colaborador, null);
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao excluir colaborador.");
                Colaborador colaborador = FindColaboradorById(sanitizedId);
                ViewBag.ErrorMessage = "Erro ao excluir. Verifique se o colaborador está associado a computadores, monitores ou periféricos.";
                return View(colaborador);
            }
        }

        private Colaborador FindColaboradorById(string id, IDbConnection connection = null, IDbTransaction transaction = null)
        {
            Colaborador colaborador = null;
            bool ownConnection = false;
            if (connection == null)
            {
                connection = _databaseService.CreateConnection();
                ownConnection = true;
            }

            var cleanId = SanitizeCpf(id);

            try
            {
                if (ownConnection)
                {
                    connection.Open();
                }

                string sql = "SELECT * FROM Colaboradores WHERE CPF = @CPF OR REPLACE(REPLACE(REPLACE(CPF, '.', ''), '-', ''), ' ', '') = @CleanCPF";
                using (var cmd = connection.CreateCommand())
                {
                    cmd.Transaction = transaction;
                    cmd.CommandText = sql;
                    var p1 = cmd.CreateParameter(); p1.ParameterName = "@CPF"; p1.Value = id ?? ""; cmd.Parameters.Add(p1);
                    var p2 = cmd.CreateParameter(); p2.ParameterName = "@CleanCPF"; p2.Value = cleanId ?? ""; cmd.Parameters.Add(p2);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            colaborador = new Colaborador
                            {
                                CPF = reader["CPF"].ToString(),
                                Nome = reader["Nome"].ToString(),
                                Email = reader["Email"].ToString(),
                                Filial = reader["Filial"].ToString(),
                                Setor = reader["Setor"].ToString(),
                                Alarme = reader["Alarme"].ToString(),
                                Videoporteiro = reader["Videoporteiro"].ToString(),
                                Obs = reader["Obs"].ToString(),
                                DataInclusao = reader["DataInclusao"] != DBNull.Value ? Convert.ToDateTime(reader["DataInclusao"]) : (DateTime?)null,
                                DataAlteracao = reader["DataAlteracao"] != DBNull.Value ? Convert.ToDateTime(reader["DataAlteracao"]) : (DateTime?)null,
                                CoordenadorCPF = reader["CoordenadorCPF"] != DBNull.Value ? reader["CoordenadorCPF"].ToString() : null
                            };
                        }
                    }

                    if (colaborador != null)
                    {
                        CarregarCredenciaisETelefones(connection, transaction, colaborador);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao encontrar colaborador por ID.");
                if (transaction != null) throw;
            }
            finally
            {
                if (ownConnection)
                {
                    connection.Close();
                }
            }
            return colaborador;
        }

        private List<Colaborador> GetCoordenadores()
        {
            var coordenadores = new List<Colaborador>();
            try
            {
                using (var connection = _databaseService.CreateConnection())
                {
                    connection.Open();
                    string sql = "SELECT c.CPF, c.Nome FROM Colaboradores c INNER JOIN Usuarios u ON c.CPF = u.ColaboradorCPF WHERE u.Role = 'Coordenador' OR u.IsCoordinator = 1 ORDER BY c.Nome";
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = sql;
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                coordenadores.Add(new Colaborador
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
                _logger.LogError(ex, "Erro ao obter a lista de coordenadores.");
            }
            return coordenadores;
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

            var colaboradores = new List<Colaborador>();
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
                            var colaborador = new Colaborador
                            {
                                CPF = SanitizeCpf(worksheet.Cells[row, 1].Value?.ToString().Trim()),
                                Nome = worksheet.Cells[row, 2].Value?.ToString().Trim(),
                                Email = worksheet.Cells[row, 3].Value?.ToString().Trim(),
                                SenhaEmail = worksheet.Cells[row, 4].Value?.ToString().Trim(),
                                Teams = worksheet.Cells[row, 5].Value?.ToString().Trim(),
                                SenhaTeams = worksheet.Cells[row, 6].Value?.ToString().Trim(),
                                EDespacho = worksheet.Cells[row, 7].Value?.ToString().Trim(),
                                SenhaEDespacho = worksheet.Cells[row, 8].Value?.ToString().Trim(),
                                Genius = worksheet.Cells[row, 9].Value?.ToString().Trim(),
                                SenhaGenius = worksheet.Cells[row, 10].Value?.ToString().Trim(),
                                Ibrooker = worksheet.Cells[row, 11].Value?.ToString().Trim(),
                                SenhaIbrooker = worksheet.Cells[row, 12].Value?.ToString().Trim(),
                                Adicional = worksheet.Cells[row, 13].Value?.ToString().Trim(),
                                SenhaAdicional = worksheet.Cells[row, 14].Value?.ToString().Trim(),
                                Filial = worksheet.Cells[row, 15].Value?.ToString().Trim(),
                                Setor = worksheet.Cells[row, 16].Value?.ToString().Trim(),
                                Smartphone = worksheet.Cells[row, 17].Value?.ToString().Trim(),
                                TelefoneFixo = worksheet.Cells[row, 18].Value?.ToString().Trim(),
                                Ramal = worksheet.Cells[row, 19].Value?.ToString().Trim(),
                                Alarme = worksheet.Cells[row, 20].Value?.ToString().Trim(),
                                Videoporteiro = worksheet.Cells[row, 21].Value?.ToString().Trim(),
                                Obs = worksheet.Cells[row, 22].Value?.ToString().Trim(),
                                CoordenadorCPF = SanitizeCpf(worksheet.Cells[row, 23].Value?.ToString().Trim())
                            };

                            if (!string.IsNullOrWhiteSpace(colaborador.CPF) && !string.IsNullOrWhiteSpace(colaborador.Nome))
                            {
                                colaboradores.Add(colaborador);
                            }
                        }
                    }
                }

                int adicionados = 0;
                int atualizados = 0;

                using (var connection = _databaseService.CreateConnection())
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            foreach (var colaborador in colaboradores)
                            {
                                var existente = FindColaboradorById(colaborador.CPF, connection, transaction);

                                if (existente != null)
                                {
                                    // Consider adding logging for bulk updates here if needed, but it might be too verbose.
                                    string updateSql = @"UPDATE Colaboradores SET 
                                                       Nome = @Nome, Email = @Email, Filial = @Filial, Setor = @Setor, Alarme = @Alarme, Videoporteiro = @Videoporteiro,
                                                       Obs = @Obs, DataAlteracao = @DataAlteracao, CoordenadorCPF = @CoordenadorCPF
                                                       WHERE CPF = @CPF";
                                    using (var cmd = connection.CreateCommand())
                                    {
                                        cmd.Transaction = transaction;
                                        cmd.CommandText = updateSql;
                                        AddColaboradorParameters(cmd, colaborador);
                                        var pDate = cmd.CreateParameter(); pDate.ParameterName = "@DataAlteracao"; pDate.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); cmd.Parameters.Add(pDate);
                                        cmd.ExecuteNonQuery();
                                    }
                                    SalvarCredenciaisETelefones(connection, transaction, colaborador);
                                    atualizados++;
                                }
                                else
                                {
                                    string insertSql = @"INSERT INTO Colaboradores (CPF, Nome, Email, Filial, Setor, Alarme, Videoporteiro, Obs, DataInclusao, CoordenadorCPF) 
                                                       VALUES (@CPF, @Nome, @Email, @Filial, @Setor, @Alarme, @Videoporteiro, @Obs, @DataInclusao, @CoordenadorCPF)";
                                    using (var cmd = connection.CreateCommand())
                                    {
                                        cmd.Transaction = transaction;
                                        cmd.CommandText = insertSql;
                                        AddColaboradorParameters(cmd, colaborador);
                                        var pDate = cmd.CreateParameter(); pDate.ParameterName = "@DataInclusao"; pDate.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); cmd.Parameters.Add(pDate);
                                        cmd.ExecuteNonQuery();
                                    }
                                    SalvarCredenciaisETelefones(connection, transaction, colaborador);
                                    adicionados++;
                                }
                            }
                            transaction.Commit();
                            TempData["SuccessMessage"] = $"{adicionados} colaboradores adicionados e {atualizados} atualizados com sucesso.";
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

        private List<string> GetDistinctColaboradorValues(IDbConnection connection, string columnName)
        {
            var values = new List<string>();
            using (var command = connection.CreateCommand())
            {
                if (columnName.Equals("Smartphone", StringComparison.OrdinalIgnoreCase) ||
                    columnName.Equals("TelefoneFixo", StringComparison.OrdinalIgnoreCase) ||
                    columnName.Equals("Ramal", StringComparison.OrdinalIgnoreCase))
                {
                    command.CommandText = "SELECT DISTINCT Numero FROM ColaboradorTelefones WHERE Tipo = @tipo AND Numero IS NOT NULL AND TRIM(Numero) != '' ORDER BY Numero";
                    var p = command.CreateParameter();
                    p.ParameterName = "@tipo";
                    p.Value = columnName;
                    command.Parameters.Add(p);
                }
                else
                {
                    command.CommandText = $"SELECT DISTINCT {columnName} FROM Colaboradores WHERE {columnName} IS NOT NULL AND TRIM({columnName}) != '' ORDER BY {columnName}";
                }

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (!reader.IsDBNull(0))
                            values.Add(reader.GetString(0));
                    }
                }
            }
            return values;
        }

        private void CarregarCredenciaisETelefones(IDbConnection connection, IDbTransaction transaction, Colaborador colab)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.Transaction = transaction;
                cmd.CommandText = "SELECT Sistema, UsuarioSistema, SenhaSistema FROM ColaboradorCredenciais WHERE ColaboradorCPF = @CPF";
                var p = cmd.CreateParameter(); p.ParameterName = "@CPF"; p.Value = colab.CPF; cmd.Parameters.Add(p);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string sistema = reader["Sistema"].ToString();
                        string usr = reader["UsuarioSistema"] != DBNull.Value ? reader["UsuarioSistema"].ToString() : null;
                        string pwd = reader["SenhaSistema"] != DBNull.Value ? reader["SenhaSistema"].ToString() : null;

                        switch (sistema)
                        {
                            case "Email": colab.Email = usr ?? colab.Email; colab.SenhaEmail = pwd; break;
                            case "Teams": colab.Teams = usr; colab.SenhaTeams = pwd; break;
                            case "EDespacho": colab.EDespacho = usr; colab.SenhaEDespacho = pwd; break;
                            case "Genius": colab.Genius = usr; colab.SenhaGenius = pwd; break;
                            case "Ibrooker": colab.Ibrooker = usr; colab.SenhaIbrooker = pwd; break;
                            case "Adicional": colab.Adicional = usr; colab.SenhaAdicional = pwd; break;
                        }
                    }
                }
            }

            using (var cmd = connection.CreateCommand())
            {
                cmd.Transaction = transaction;
                cmd.CommandText = "SELECT Tipo, Numero FROM ColaboradorTelefones WHERE ColaboradorCPF = @CPF";
                var p = cmd.CreateParameter(); p.ParameterName = "@CPF"; p.Value = colab.CPF; cmd.Parameters.Add(p);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string tipo = reader["Tipo"].ToString();
                        string num = reader["Numero"].ToString();

                        switch (tipo)
                        {
                            case "Smartphone": colab.Smartphone = num; break;
                            case "TelefoneFixo": colab.TelefoneFixo = num; break;
                            case "Ramal": colab.Ramal = num; break;
                        }
                    }
                }
            }
        }

        private void SalvarCredenciaisETelefones(IDbConnection connection, IDbTransaction transaction, Colaborador colab)
        {
            // Credenciais
            using (var delCmd = connection.CreateCommand())
            {
                delCmd.Transaction = transaction;
                delCmd.CommandText = "DELETE FROM ColaboradorCredenciais WHERE ColaboradorCPF = @CPF";
                var p = delCmd.CreateParameter(); p.ParameterName = "@CPF"; p.Value = colab.CPF; delCmd.Parameters.Add(p);
                delCmd.ExecuteNonQuery();
            }

            var creds = new (string Sistema, string? Usuario, string? Senha)[]
            {
                ("Email", colab.Email, colab.SenhaEmail),
                ("Teams", colab.Teams, colab.SenhaTeams),
                ("EDespacho", colab.EDespacho, colab.SenhaEDespacho),
                ("Genius", colab.Genius, colab.SenhaGenius),
                ("Ibrooker", colab.Ibrooker, colab.SenhaIbrooker),
                ("Adicional", colab.Adicional, colab.SenhaAdicional)
            };

            foreach (var (sistema, usuario, senha) in creds)
            {
                if (!string.IsNullOrWhiteSpace(senha) || !string.IsNullOrWhiteSpace(usuario))
                {
                    using (var insCmd = connection.CreateCommand())
                    {
                        insCmd.Transaction = transaction;
                        insCmd.CommandText = "INSERT INTO ColaboradorCredenciais (ColaboradorCPF, Sistema, UsuarioSistema, SenhaSistema) VALUES (@CPF, @Sistema, @Usuario, @Senha)";
                        var p1 = insCmd.CreateParameter(); p1.ParameterName = "@CPF"; p1.Value = colab.CPF; insCmd.Parameters.Add(p1);
                        var p2 = insCmd.CreateParameter(); p2.ParameterName = "@Sistema"; p2.Value = sistema; insCmd.Parameters.Add(p2);
                        var p3 = insCmd.CreateParameter(); p3.ParameterName = "@Usuario"; p3.Value = (object)usuario ?? DBNull.Value; insCmd.Parameters.Add(p3);
                        var p4 = insCmd.CreateParameter(); p4.ParameterName = "@Senha"; p4.Value = (object)senha ?? DBNull.Value; insCmd.Parameters.Add(p4);
                        insCmd.ExecuteNonQuery();
                    }
                }
            }

            // Telefones
            using (var delCmd = connection.CreateCommand())
            {
                delCmd.Transaction = transaction;
                delCmd.CommandText = "DELETE FROM ColaboradorTelefones WHERE ColaboradorCPF = @CPF";
                var p = delCmd.CreateParameter(); p.ParameterName = "@CPF"; p.Value = colab.CPF; delCmd.Parameters.Add(p);
                delCmd.ExecuteNonQuery();
            }

            var tels = new (string Tipo, string? Numero)[]
            {
                ("Smartphone", colab.Smartphone),
                ("TelefoneFixo", colab.TelefoneFixo),
                ("Ramal", colab.Ramal)
            };

            foreach (var (tipo, numero) in tels)
            {
                if (!string.IsNullOrWhiteSpace(numero))
                {
                    using (var insCmd = connection.CreateCommand())
                    {
                        insCmd.Transaction = transaction;
                        insCmd.CommandText = "INSERT INTO ColaboradorTelefones (ColaboradorCPF, Tipo, Numero) VALUES (@CPF, @Tipo, @Numero)";
                        var p1 = insCmd.CreateParameter(); p1.ParameterName = "@CPF"; p1.Value = colab.CPF; insCmd.Parameters.Add(p1);
                        var p2 = insCmd.CreateParameter(); p2.ParameterName = "@Tipo"; p2.Value = tipo; insCmd.Parameters.Add(p2);
                        var p3 = insCmd.CreateParameter(); p3.ParameterName = "@Numero"; p3.Value = numero; insCmd.Parameters.Add(p3);
                        insCmd.ExecuteNonQuery();
                    }
                }
            }
        }

        private void AddColaboradorParameters(IDbCommand cmd, Colaborador colaborador)
        {
            var p1 = cmd.CreateParameter(); p1.ParameterName = "@CPF"; p1.Value = colaborador.CPF; cmd.Parameters.Add(p1);
            var p2 = cmd.CreateParameter(); p2.ParameterName = "@Nome"; p2.Value = colaborador.Nome; cmd.Parameters.Add(p2);
            var p3 = cmd.CreateParameter(); p3.ParameterName = "@Email"; p3.Value = (object)colaborador.Email ?? DBNull.Value; cmd.Parameters.Add(p3);
            var p15 = cmd.CreateParameter(); p15.ParameterName = "@Filial"; p15.Value = (object)colaborador.Filial ?? DBNull.Value; cmd.Parameters.Add(p15);
            var p16 = cmd.CreateParameter(); p16.ParameterName = "@Setor"; p16.Value = (object)colaborador.Setor ?? DBNull.Value; cmd.Parameters.Add(p16);
            var p20 = cmd.CreateParameter(); p20.ParameterName = "@Alarme"; p20.Value = (object)colaborador.Alarme ?? DBNull.Value; cmd.Parameters.Add(p20);
            var p21 = cmd.CreateParameter(); p21.ParameterName = "@Videoporteiro"; p21.Value = (object)colaborador.Videoporteiro ?? DBNull.Value; cmd.Parameters.Add(p21);
            var p22 = cmd.CreateParameter(); p22.ParameterName = "@Obs"; p22.Value = (object)colaborador.Obs ?? DBNull.Value; cmd.Parameters.Add(p22);
            var p23 = cmd.CreateParameter(); p23.ParameterName = "@CoordenadorCPF"; p23.Value = (object)colaborador.CoordenadorCPF ?? DBNull.Value; cmd.Parameters.Add(p23);
        }
    }
}
