using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using System.Linq;
using System.Security.Claims;
using Web.Models;
using Microsoft.AspNetCore.Authorization;
using Web.Services;
using System.Data;
using OfficeOpenXml;
using System.IO;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin,Coordenador")]
    public class ExportarController : Controller
    {
        private readonly IDatabaseService _databaseService;
        private readonly ILogger<ExportarController> _logger;
        private readonly PersistentLogService _persistentLogService;

        public ExportarController(IDatabaseService databaseService, ILogger<ExportarController> logger, PersistentLogService persistentLogService)
        {
            _databaseService = databaseService;
            _logger = logger;
            _persistentLogService = persistentLogService;
        }

        public IActionResult Index()
        {
            var viewModel = new ExportarViewModel();
            using (var connection = _databaseService.CreateConnection())
            {
                connection.Open();
                // Computer filters
                viewModel.Fabricantes = GetDistinctValues(connection, "Computadores", "Fabricante");
                viewModel.SOs = GetDistinctValues(connection, "Computadores", "SO");
                viewModel.ProcessadorFabricantes = GetDistinctValues(connection, "Computadores", "ProcessadorFabricante");
                viewModel.RamTipos = GetDistinctValues(connection, "Computadores", "RamTipo");
                viewModel.Processadores = GetDistinctValues(connection, "Computadores", "Processador");
                viewModel.Rams = GetDistinctValues(connection, "Computadores", "Ram");

                // Monitor filters
                viewModel.Marcas = GetDistinctValues(connection, "Monitores", "Marca");
                viewModel.Tamanhos = GetDistinctValues(connection, "Monitores", "Tamanho");
                viewModel.Modelos = GetDistinctValues(connection, "Monitores", "Modelo");

                // Periferico filters
                viewModel.TiposPeriferico = GetDistinctValues(connection, "Perifericos", "Tipo");

                // Colaborador filter
                viewModel.Colaboradores = GetColaboradores(connection);
                viewModel.Coordenadores = GetCoordenadores(connection);
            }
            return View(viewModel);
        }

        private List<Colaborador> GetCoordenadores(IDbConnection connection)
        {
            var coordenadores = new List<Colaborador>();
            string sql;

            if (User.IsInRole("Admin"))
            {
                sql = "SELECT c.CPF, c.Nome FROM Colaboradores c INNER JOIN Usuarios u ON c.CPF = u.ColaboradorCPF WHERE u.Role = 'Coordenador' OR u.IsCoordinator = 1 ORDER BY c.Nome";
            }
            else
            {
                // Note: Parameters are not directly supported in SQL string here, need to handle in command execution
                sql = "SELECT c.CPF, c.Nome FROM Colaboradores c WHERE c.CPF = @UserCPF";
            }

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = sql;
                if (!User.IsInRole("Admin"))
                {
                    var userCpf = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var p = cmd.CreateParameter(); p.ParameterName = "@UserCPF"; p.Value = userCpf; cmd.Parameters.Add(p);
                }

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
            return coordenadores;
        }

        private List<Colaborador> GetColaboradores(IDbConnection connection)
        {
            var colaboradores = new List<Colaborador>();
            string sql = "SELECT CPF, Nome FROM Colaboradores";

            if (!User.IsInRole("Admin") && !User.IsInRole("Diretoria"))
            {
                if (User.IsInRole("Coordenador") || User.IsInRole("Colaborador"))
                {
                    sql += " WHERE CoordenadorCPF = @userCpf OR CPF = @userCpf";
                }
            }

            sql += " ORDER BY Nome";

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = sql;
                
                if (sql.Contains("@userCpf"))
                {
                    var userCpf = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var p = cmd.CreateParameter();
                    p.ParameterName = "@userCpf";
                    p.Value = userCpf ?? (object)DBNull.Value;
                    cmd.Parameters.Add(p);
                }

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
            return colaboradores;
        }

        private List<string> GetDistinctValues(IDbConnection connection, string tableName, string columnName)
        {
            var values = new List<string>();
            var sql = $"SELECT DISTINCT {columnName} FROM {tableName} WHERE {columnName} IS NOT NULL ORDER BY {columnName}";
            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        values.Add(reader[0].ToString());
                    }
                }
            }
            return values;
        }

        [HttpPost]
        public async Task<IActionResult> Export(ExportarViewModel viewModel)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            string fileName = $"export_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
            string exportDetails = "";
            var userCpf = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            bool isRestricted = !User.IsInRole("Admin") && !User.IsInRole("Diretoria") && (User.IsInRole("Coordenador") || User.IsInRole("Colaborador"));

            using (var package = new ExcelPackage())
            using (var connection = _databaseService.CreateConnection())
            {
                connection.Open();

                if (viewModel.ExportMode == ExportMode.PorDispositivo)
                {
                    exportDetails = $"Exported by Device Type: {viewModel.DeviceType}";
                    fileName = $"export_{viewModel.DeviceType}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                    string sql = "";
                    var whereClauses = new List<string>();
                    var parameters = new Dictionary<string, object>();

                    Action<string, List<string>> addInClause = (columnName, values) =>
                    {
                        if (values != null && values.Any())
                        {
                            var paramNames = new List<string>();
                            for (int i = 0; i < values.Count; i++)
                            {
                                var paramName = $"@{columnName.ToLower().Replace(" ", "")}{i}";
                                paramNames.Add(paramName);
                                parameters.Add(paramName, values[i]);
                            }
                            whereClauses.Add($"{columnName} IN ({string.Join(", ", paramNames)})");
                        }
                    };

                    var worksheet = package.Workbook.Worksheets.Add("Dados");

                    switch (viewModel.DeviceType)
                    {
                        case DeviceType.Computadores:
                            addInClause("c.Fabricante", viewModel.CurrentFabricantes);
                            addInClause("c.SO", viewModel.CurrentSOs);
                            addInClause("c.ProcessadorFabricante", viewModel.CurrentProcessadorFabricantes);
                            addInClause("c.RamTipo", viewModel.CurrentRamTipos);
                            addInClause("c.Processador", viewModel.CurrentProcessadores);
                            addInClause("c.Ram", viewModel.CurrentRams);

                            if (isRestricted)
                            {
                                whereClauses.Add("(c.ColaboradorCPF = @userCpf OR col.CoordenadorCPF = @userCpf)");
                                parameters.Add("@userCpf", userCpf);
                            }

                            string[] computerHeader = { "MAC", "IP", "ColaboradorCPF", "Hostname", "Fabricante", "Processador", "ProcessadorFabricante", "ProcessadorCore", "ProcessadorThread", "ProcessadorClock", "Ram", "RamTipo", "RamVelocidade", "RamVoltagem", "RamPorModule", "ArmazenamentoC", "ArmazenamentoCTotal", "ArmazenamentoCLivre", "ArmazenamentoD", "ArmazenamentoDTotal", "ArmazenamentoDLivre", "ConsumoCPU", "SO", "PartNumber" };
                            for (int i = 0; i < computerHeader.Length; i++) worksheet.Cells[1, i + 1].Value = computerHeader[i];

                            sql = $"SELECT c.* FROM Computadores c" + (isRestricted ? " LEFT JOIN Colaboradores col ON c.ColaboradorCPF = col.CPF" : "");

                            if (whereClauses.Any())
                            {
                                sql += " WHERE " + string.Join(" AND ", whereClauses);
                            }

                            using (var cmd = connection.CreateCommand())
                            {
                                cmd.CommandText = sql;
                                foreach (var p in parameters)
                                {
                                    var param = cmd.CreateParameter(); param.ParameterName = p.Key; param.Value = p.Value; cmd.Parameters.Add(param);
                                }

                                using (var reader = cmd.ExecuteReader())
                                {
                                    int row = 2;
                                    while (reader.Read())
                                    {
                                        for (int i = 0; i < computerHeader.Length; i++)
                                        {
                                            try {
                                                worksheet.Cells[row, i + 1].Value = reader[computerHeader[i]].ToString();
                                            } catch {
                                                worksheet.Cells[row, i + 1].Value = "";
                                            }
                                        }
                                        row++;
                                    }
                                }
                            }
                            break;

                        case DeviceType.Monitores:
                            addInClause("m.Marca", viewModel.CurrentMarcas);
                            addInClause("m.Tamanho", viewModel.CurrentTamanhos);
                            addInClause("m.Modelo", viewModel.CurrentModelos);

                            if (isRestricted)
                            {
                                whereClauses.Add("(m.ColaboradorCPF = @userCpf OR col.CoordenadorCPF = @userCpf)");
                                parameters.Add("@userCpf", userCpf);
                            }

                            string[] monitorHeader = { "PartNumber", "ColaboradorCPF", "Marca", "Modelo", "Tamanho" };
                            for (int i = 0; i < monitorHeader.Length; i++) worksheet.Cells[1, i + 1].Value = monitorHeader[i];

                            sql = $"SELECT m.* FROM Monitores m" + (isRestricted ? " LEFT JOIN Colaboradores col ON m.ColaboradorCPF = col.CPF" : "");
                            if (whereClauses.Any())
                            {
                                sql += " WHERE " + string.Join(" AND ", whereClauses);
                            }

                            using (var cmd = connection.CreateCommand())
                            {
                                cmd.CommandText = sql;
                                foreach (var p in parameters)
                                {
                                    var param = cmd.CreateParameter(); param.ParameterName = p.Key; param.Value = p.Value; cmd.Parameters.Add(param);
                                }

                                using (var reader = cmd.ExecuteReader())
                                {
                                    int row = 2;
                                    while (reader.Read())
                                    {
                                        for (int i = 0; i < monitorHeader.Length; i++)
                                        {
                                            try {
                                                worksheet.Cells[row, i + 1].Value = reader[monitorHeader[i]].ToString();
                                            } catch {
                                                worksheet.Cells[row, i + 1].Value = "";
                                            }
                                        }
                                        row++;
                                    }
                                }
                            }
                            break;

                        case DeviceType.Perifericos:
                            addInClause("p.Tipo", viewModel.CurrentTiposPeriferico);

                            if (isRestricted)
                            {
                                whereClauses.Add("(p.ColaboradorCPF = @userCpf OR col.CoordenadorCPF = @userCpf)");
                                parameters.Add("@userCpf", userCpf);
                            }

                            string[] perifericoHeader = { "PartNumber", "ColaboradorCPF", "Tipo", "DataEntrega" };
                            for (int i = 0; i < perifericoHeader.Length; i++) worksheet.Cells[1, i + 1].Value = perifericoHeader[i];

                            sql = $"SELECT p.* FROM Perifericos p" + (isRestricted ? " LEFT JOIN Colaboradores col ON p.ColaboradorCPF = col.CPF" : "");
                            if (whereClauses.Any())
                            {
                                sql += " WHERE " + string.Join(" AND ", whereClauses);
                            }

                            using (var cmd = connection.CreateCommand())
                            {
                                cmd.CommandText = sql;
                                foreach (var p in parameters)
                                {
                                    var param = cmd.CreateParameter(); param.ParameterName = p.Key; param.Value = p.Value; cmd.Parameters.Add(param);
                                }

                                using (var reader = cmd.ExecuteReader())
                                {
                                    int row = 2;
                                    while (reader.Read())
                                    {
                                        for (int i = 0; i < perifericoHeader.Length; i++)
                                        {
                                            try {
                                                if (perifericoHeader[i] == "DataEntrega" && reader[perifericoHeader[i]] != DBNull.Value)
                                                    worksheet.Cells[row, i + 1].Value = Convert.ToDateTime(reader[perifericoHeader[i]]).ToString("yyyy-MM-dd HH:mm:ss");
                                                else
                                                    worksheet.Cells[row, i + 1].Value = reader[perifericoHeader[i]].ToString();
                                            } catch {
                                                worksheet.Cells[row, i + 1].Value = "";
                                            }
                                        }
                                        row++;
                                    }
                                }
                            }
                            break;

                        case DeviceType.Colaboradores:
                            string[] colabHeader = { "CPF", "Nome", "Email", "SenhaEmail", "Teams", "SenhaTeams", "EDespacho", "SenhaEDespacho", "Genius", "SenhaGenius", "Ibrooker", "SenhaIbrooker", "Adicional", "SenhaAdicional", "Filial", "Setor", "Smartphone", "TelefoneFixo", "Ramal", "Alarme", "Videoporteiro", "Obs", "CoordenadorCPF" };
                            for (int i = 0; i < colabHeader.Length; i++) worksheet.Cells[1, i + 1].Value = colabHeader[i];

                            sql = $"SELECT * FROM Colaboradores";

                            if (isRestricted)
                            {
                                sql += " WHERE CoordenadorCPF = @userCpf OR CPF = @userCpf";
                                parameters.Add("@userCpf", userCpf);
                            }

                            using (var cmd = connection.CreateCommand())
                            {
                                cmd.CommandText = sql;
                                foreach (var p in parameters)
                                {
                                    var param = cmd.CreateParameter(); param.ParameterName = p.Key; param.Value = p.Value; cmd.Parameters.Add(param);
                                }

                                using (var reader = cmd.ExecuteReader())
                                {
                                    int row = 2;
                                    while (reader.Read())
                                    {
                                        for (int i = 0; i < colabHeader.Length; i++)
                                        {
                                            try {
                                                worksheet.Cells[row, i + 1].Value = reader[colabHeader[i]].ToString();
                                            } catch {
                                                worksheet.Cells[row, i + 1].Value = "";
                                            }
                                        }
                                        row++;
                                    }
                                }
                            }
                            break;
                    }
                }
                else if (viewModel.ExportMode == ExportMode.PorColaborador)
                {
                    if (isRestricted)
                    {
                        bool isAuthorized = false;
                        using (var authCmd = connection.CreateCommand())
                        {
                            authCmd.CommandText = "SELECT 1 FROM Colaboradores WHERE CPF = @targetCpf AND (CoordenadorCPF = @userCpf OR CPF = @userCpf)";
                            var pTarget = authCmd.CreateParameter(); pTarget.ParameterName = "@targetCpf"; pTarget.Value = viewModel.SelectedColaboradorCPF; authCmd.Parameters.Add(pTarget);
                            var pUser = authCmd.CreateParameter(); pUser.ParameterName = "@userCpf"; pUser.Value = userCpf; authCmd.Parameters.Add(pUser);
                            var result = authCmd.ExecuteScalar();
                            isAuthorized = result != null;
                        }
                        if (!isAuthorized) return Unauthorized();
                    }

                    exportDetails = $"Exported by Collaborator CPF: {viewModel.SelectedColaboradorCPF}";
                    fileName = $"export_colaborador_{viewModel.SelectedColaboradorCPF}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

                    var wsComputadores = package.Workbook.Worksheets.Add("Computadores");
                    string[] computerHeader = { "MAC", "IP", "ColaboradorCPF", "Hostname", "Fabricante", "Processador", "ProcessadorFabricante", "ProcessadorCore", "ProcessadorThread", "ProcessadorClock", "Ram", "RamTipo", "RamVelocidade", "RamVoltagem", "RamPorModule", "ArmazenamentoC", "ArmazenamentoCTotal", "ArmazenamentoCLivre", "ArmazenamentoD", "ArmazenamentoDTotal", "ArmazenamentoDLivre", "ConsumoCPU", "SO", "PartNumber" };
                    for (int i = 0; i < computerHeader.Length; i++) wsComputadores.Cells[1, i + 1].Value = computerHeader[i];

                    string sqlComputadores = "SELECT * FROM Computadores c WHERE c.ColaboradorCPF = @colaboradorCpf";
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = sqlComputadores;
                        var p = cmd.CreateParameter(); p.ParameterName = "@colaboradorCpf"; p.Value = viewModel.SelectedColaboradorCPF; cmd.Parameters.Add(p);
                        using (var reader = cmd.ExecuteReader())
                        {
                            int row = 2;
                            while (reader.Read())
                            {
                                for (int i = 0; i < computerHeader.Length; i++)
                                {
                                    try { wsComputadores.Cells[row, i + 1].Value = reader[computerHeader[i]].ToString(); }
                                    catch { wsComputadores.Cells[row, i + 1].Value = ""; }
                                }
                                row++;
                            }
                        }
                    }

                    var wsMonitores = package.Workbook.Worksheets.Add("Monitores");
                    string[] monitorHeader = { "PartNumber", "ColaboradorCPF", "Marca", "Modelo", "Tamanho" };
                    for (int i = 0; i < monitorHeader.Length; i++) wsMonitores.Cells[1, i + 1].Value = monitorHeader[i];

                    string sqlMonitores = "SELECT * FROM Monitores m WHERE m.ColaboradorCPF = @colaboradorCpf";
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = sqlMonitores;
                        var p = cmd.CreateParameter(); p.ParameterName = "@colaboradorCpf"; p.Value = viewModel.SelectedColaboradorCPF; cmd.Parameters.Add(p);
                        using (var reader = cmd.ExecuteReader())
                        {
                            int row = 2;
                            while (reader.Read())
                            {
                                for (int i = 0; i < monitorHeader.Length; i++)
                                {
                                    try { wsMonitores.Cells[row, i + 1].Value = reader[monitorHeader[i]].ToString(); }
                                    catch { wsMonitores.Cells[row, i + 1].Value = ""; }
                                }
                                row++;
                            }
                        }
                    }

                    var wsPerifericos = package.Workbook.Worksheets.Add("Perifericos");
                    string[] perifericoHeader = { "PartNumber", "ColaboradorCPF", "Tipo", "DataEntrega" };
                    for (int i = 0; i < perifericoHeader.Length; i++) wsPerifericos.Cells[1, i + 1].Value = perifericoHeader[i];

                    string sqlPerifericos = "SELECT * FROM Perifericos p WHERE p.ColaboradorCPF = @colaboradorCpf";
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = sqlPerifericos;
                        var p = cmd.CreateParameter(); p.ParameterName = "@colaboradorCpf"; p.Value = viewModel.SelectedColaboradorCPF; cmd.Parameters.Add(p);
                        using (var reader = cmd.ExecuteReader())
                        {
                            int row = 2;
                            while (reader.Read())
                            {
                                for (int i = 0; i < perifericoHeader.Length; i++)
                                {
                                    try
                                    {
                                        if (perifericoHeader[i] == "DataEntrega" && reader[perifericoHeader[i]] != DBNull.Value)
                                            wsPerifericos.Cells[row, i + 1].Value = Convert.ToDateTime(reader[perifericoHeader[i]]).ToString("yyyy-MM-dd HH:mm:ss");
                                        else
                                            wsPerifericos.Cells[row, i + 1].Value = reader[perifericoHeader[i]].ToString();
                                    }
                                    catch { wsPerifericos.Cells[row, i + 1].Value = ""; }
                                }
                                row++;
                            }
                        }
                    }

                    var wsManutencoes = package.Workbook.Worksheets.Add("Manutencoes");
                    string[] manutencaoHeader = { "Equipamento", "Tipo", "DataManutencaoHardware", "DataManutencaoSoftware", "ManutencaoExterna", "Data", "Historico" };
                    for (int i = 0; i < manutencaoHeader.Length; i++) wsManutencoes.Cells[1, i + 1].Value = manutencaoHeader[i];

                    string sqlManutencoes = @"
                        SELECT
                            COALESCE(c.MAC, m.PartNumber, p.PartNumber) as Equipamento,
                            CASE
                                WHEN c.MAC IS NOT NULL THEN 'Computador'
                                WHEN m.PartNumber IS NOT NULL THEN 'Monitor'
                                WHEN p.PartNumber IS NOT NULL THEN 'Periferico'
                            END as Tipo,
                            ma.DataManutencaoHardware,
                            ma.DataManutencaoSoftware,
                            ma.ManutencaoExterna,
                            ma.Data,
                            ma.Historico
                        FROM Manutencoes ma
                        LEFT JOIN Computadores c ON ma.ComputadorMAC = c.MAC AND c.ColaboradorCPF = @colaboradorCpf
                        LEFT JOIN Monitores m ON ma.MonitorPartNumber = m.PartNumber AND m.ColaboradorCPF = @colaboradorCpf
                        LEFT JOIN Perifericos p ON ma.PerifericoPartNumber = p.PartNumber AND p.ColaboradorCPF = @colaboradorCpf
                        WHERE c.ColaboradorCPF = @colaboradorCpf OR m.ColaboradorCPF = @colaboradorCpf OR p.ColaboradorCPF = @colaboradorCpf";

                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = sqlManutencoes;
                        var p = cmd.CreateParameter(); p.ParameterName = "@colaboradorCpf"; p.Value = viewModel.SelectedColaboradorCPF; cmd.Parameters.Add(p);
                        using (var reader = cmd.ExecuteReader())
                        {
                            int row = 2;
                            while (reader.Read())
                            {
                                for (int i = 0; i < manutencaoHeader.Length; i++)
                                {
                                    try { wsManutencoes.Cells[row, i + 1].Value = reader[manutencaoHeader[i]].ToString(); }
                                    catch { wsManutencoes.Cells[row, i + 1].Value = ""; }
                                }
                                row++;
                            }
                        }
                    }
                }
                else if (viewModel.ExportMode == ExportMode.PorCoordenador)
                {
                    if (isRestricted && viewModel.CoordenadorCPF != userCpf)
                    {
                        return Unauthorized();
                    }

                    exportDetails = $"Exported by Coordinator CPF: {viewModel.CoordenadorCPF}";
                    fileName = $"export_coordenador_{viewModel.CoordenadorCPF}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                    var wsComputadores = package.Workbook.Worksheets.Add("Computadores");

                    string[] computerHeader = { "MAC", "IP", "ColaboradorCPF", "Hostname", "Fabricante", "Processador", "ProcessadorFabricante", "ProcessadorCore", "ProcessadorThread", "ProcessadorClock", "Ram", "RamTipo", "RamVelocidade", "RamVoltagem", "RamPorModule", "ArmazenamentoC", "ArmazenamentoCTotal", "ArmazenamentoCLivre", "ArmazenamentoD", "ArmazenamentoDTotal", "ArmazenamentoDLivre", "ConsumoCPU", "SO", "PartNumber" };
                    for (int i = 0; i < computerHeader.Length; i++) wsComputadores.Cells[1, i + 1].Value = computerHeader[i];

                    string sql = $@"
                        SELECT c.*
                        FROM Computadores c
                        INNER JOIN Colaboradores colab ON c.ColaboradorCPF = colab.CPF
                        WHERE colab.CoordenadorCPF = @coordenadorCpf OR colab.CPF = @coordenadorCpf";

                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = sql;
                        var p = cmd.CreateParameter(); p.ParameterName = "@coordenadorCpf"; p.Value = viewModel.CoordenadorCPF; cmd.Parameters.Add(p);

                        using (var reader = cmd.ExecuteReader())
                        {
                            int row = 2;
                            while (reader.Read())
                            {
                                for (int i = 0; i < computerHeader.Length; i++)
                                {
                                    try {
                                        wsComputadores.Cells[row, i + 1].Value = reader[computerHeader[i]].ToString();
                                    } catch {
                                        wsComputadores.Cells[row, i + 1].Value = "";
                                    }
                                }
                                row++;
                            }
                        }
                    }
                }

                await _persistentLogService.LogChangeAsync(
                    User.Identity.Name,
                    "EXPORT",
                    "Data",
                    "Exported data to Excel",
                    exportDetails
                );

                using (var stream = new MemoryStream())
                {
                    package.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
        }
    }
}
