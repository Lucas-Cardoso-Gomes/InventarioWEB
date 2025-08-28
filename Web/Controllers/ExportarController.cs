using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using web.Models;
using Web.Models;
using Microsoft.AspNetCore.Authorization;
using Web.Services;
using System.Threading.Tasks;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin,Coordenador")]
    public class ExportarController : Controller
    {
        private readonly string _connectionString;
        private readonly ILogger<ExportarController> _logger;
        private readonly UserService _userService;

        public ExportarController(IConfiguration configuration, ILogger<ExportarController> logger, UserService userService)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
            _userService = userService;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new ExportarViewModel();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var whereClauses = new List<string>();
                var parameters = new Dictionary<string, object>();

                var user = await _userService.FindByLoginAsync(User.Identity.Name);
                if (User.IsInRole("Coordenador"))
                {
                    var colaboradores = await _userService.GetColaboradoresByCoordenadorAsync(user.Id);
                    var cpfs = colaboradores.Select(c => c.ColaboradorCPF).ToList();
                    if (user.ColaboradorCPF != null)
                    {
                        cpfs.Add(user.ColaboradorCPF);
                    }
                    if (cpfs.Any())
                    {
                        var cpfParams = new List<string>();
                        for (int i = 0; i < cpfs.Count; i++)
                        {
                            var paramName = $"@cpf{i}";
                            cpfParams.Add(paramName);
                            parameters.Add(paramName, cpfs[i]);
                        }
                        whereClauses.Add($"ColaboradorCPF IN ({string.Join(", ", cpfParams)})");
                    }
                }

                string whereSql = whereClauses.Any() ? $"WHERE {string.Join(" AND ", whereClauses)}" : "";

                // Computer filters
                viewModel.Fabricantes = GetDistinctValues(connection, "Computadores", "Fabricante", whereSql, parameters);
                viewModel.SOs = GetDistinctValues(connection, "Computadores", "SO", whereSql, parameters);
                viewModel.ProcessadorFabricantes = GetDistinctValues(connection, "Computadores", "ProcessadorFabricante", whereSql, parameters);
                viewModel.RamTipos = GetDistinctValues(connection, "Computadores", "RamTipo", whereSql, parameters);
                viewModel.Processadores = GetDistinctValues(connection, "Computadores", "Processador", whereSql, parameters);
                viewModel.Rams = GetDistinctValues(connection, "Computadores", "Ram", whereSql, parameters);

                // Monitor filters
                viewModel.Marcas = GetDistinctValues(connection, "Monitores", "Marca", whereSql, parameters);
                viewModel.Tamanhos = GetDistinctValues(connection, "Monitores", "Tamanho", whereSql, parameters);
                viewModel.Modelos = GetDistinctValues(connection, "Monitores", "Modelo", whereSql, parameters);

                // Periferico filters
                viewModel.TiposPeriferico = GetDistinctValues(connection, "Perifericos", "Tipo", whereSql, parameters);

                // Colaborador filter
                viewModel.Colaboradores = GetColaboradores(connection, whereSql, parameters).Select(c => c.Nome).ToList();

                if (User.IsInRole("Admin") || User.IsInRole("Diretoria") || User.IsInRole("RH"))
                {
                    viewModel.AllCoordenadores = GetAllCoordenadores();
                }
            }
            return View(viewModel);
        }

        private List<string> GetAllCoordenadores()
        {
            var coordenadores = new List<string>();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string sql = "SELECT Nome FROM Usuarios WHERE Role = 'Coordenador' ORDER BY Nome";
                using (var cmd = new SqlCommand(sql, connection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            coordenadores.Add(reader["Nome"].ToString());
                        }
                    }
                }
            }
            return coordenadores;
        }

        private List<Colaborador> GetColaboradores(SqlConnection connection, string whereSql, Dictionary<string, object> parameters)
        {
            var colaboradores = new List<Colaborador>();
            string sql = $"SELECT CPF, Nome FROM Colaboradores {whereSql} ORDER BY Nome";
            using (var cmd = new SqlCommand(sql, connection))
            {
                foreach (var p in parameters)
                {
                    cmd.Parameters.AddWithValue(p.Key, p.Value);
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

        private List<string> GetDistinctValues(SqlConnection connection, string tableName, string columnName, string whereSql, Dictionary<string, object> parameters)
        {
            var values = new List<string>();
            var sql = $"SELECT DISTINCT {columnName} FROM {tableName} ";
            if (!string.IsNullOrEmpty(whereSql))
            {
                sql += whereSql + " AND ";
            }
            else
            {
                sql += " WHERE ";
            }
            sql += $"{columnName} IS NOT NULL ORDER BY {columnName}";

            using (var command = new SqlCommand(sql, connection))
            {
                foreach (var p in parameters)
                {
                    command.Parameters.AddWithValue(p.Key, p.Value);
                }
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
            var csvBuilder = new StringBuilder();
            string fileName = $"export_{DateTime.Now:yyyyMMddHHmmss}.csv";

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var whereClauses = new List<string>();
                var parameters = new Dictionary<string, object>();

                var user = await _userService.FindByLoginAsync(User.Identity.Name);
                if (User.IsInRole("Coordenador"))
                {
                    var colaboradores = await _userService.GetColaboradoresByCoordenadorAsync(user.Id);
                    var cpfs = colaboradores.Select(c => c.ColaboradorCPF).ToList();
                    if (user.ColaboradorCPF != null)
                    {
                        cpfs.Add(user.ColaboradorCPF);
                    }
                    if (cpfs.Any())
                    {
                        var cpfParams = new List<string>();
                        for (int i = 0; i < cpfs.Count; i++)
                        {
                            var paramName = $"@cpf{i}";
                            cpfParams.Add(paramName);
                            parameters.Add(paramName, cpfs[i]);
                        }
                        whereClauses.Add($"ColaboradorCPF IN ({string.Join(", ", cpfParams)})");
                    }
                }

                if (viewModel.ExportMode == ExportMode.PorDispositivo)
                {
                    fileName = $"export_{viewModel.DeviceType}_{DateTime.Now:yyyyMMddHHmmss}.csv";
                    string sql = "";

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

                    switch (viewModel.DeviceType)
                    {
                        case DeviceType.Computadores:
                            addInClause("Fabricante", viewModel.CurrentFabricantes);
                            addInClause("SO", viewModel.CurrentSOs);
                            addInClause("ProcessadorFabricante", viewModel.CurrentProcessadorFabricantes);
                            addInClause("RamTipo", viewModel.CurrentRamTipos);
                            addInClause("Processador", viewModel.CurrentProcessadores);
                            addInClause("Ram", viewModel.CurrentRams);

                            string computerHeader = "MAC,IP,ColaboradorNome,Hostname,Fabricante,Processador,ProcessadorFabricante,ProcessadorCore,ProcessadorThread,ProcessadorClock,Ram,RamTipo,RamVelocidade,RamVoltagem,RamPorModule,ArmazenamentoC,ArmazenamentoCTotal,ArmazenamentoCLivre,ArmazenamentoD,ArmazenamentoDTotal,ArmazenamentoDLivre,ConsumoCPU,SO,DataColeta";
                            csvBuilder.AppendLine(computerHeader);
                            string whereSqlComputadores = whereClauses.Any() ? $"WHERE {string.Join(" AND ", whereClauses)}" : "";
                            sql = $"SELECT {computerHeader} FROM Computadores {whereSqlComputadores}";
                            
                            using (var cmd = new SqlCommand(sql, connection))
                            {
                                foreach (var p in parameters)
                                {
                                    cmd.Parameters.AddWithValue(p.Key, p.Value);
                                }

                                using (var reader = cmd.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        var line = new List<string>();
                                        foreach (var col in computerHeader.Split(','))
                                        {
                                            line.Add(reader[col].ToString());
                                        }
                                        csvBuilder.AppendLine(string.Join(",", line));

                                        // Manutenções
                                        string mac = reader["MAC"].ToString();
                                        string sqlManutencoes = "SELECT * FROM Manutencoes WHERE ComputadorMAC = @MAC";
                                        using (var manutencaoConnection = new SqlConnection(_connectionString))
                                        {
                                            manutencaoConnection.Open();
                                            using (var manutencaoCmd = new SqlCommand(sqlManutencoes, manutencaoConnection))
                                            {
                                                manutencaoCmd.Parameters.AddWithValue("@MAC", mac);
                                                using (var manutencaoReader = manutencaoCmd.ExecuteReader())
                                                {
                                                    if (manutencaoReader.HasRows)
                                                    {
                                                        csvBuilder.AppendLine("Manutenções do Computador:");
                                                        csvBuilder.AppendLine("ID,Data,Descricao,Custo");
                                                        while (manutencaoReader.Read())
                                                        {
                                                            csvBuilder.AppendLine($"{manutencaoReader["Id"]},{manutencaoReader["Data"]},{manutencaoReader["Descricao"]},{manutencaoReader["Custo"]}");
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            break;

                        case DeviceType.Monitores:
                            addInClause("Marca", viewModel.CurrentMarcas);
                            addInClause("Tamanho", viewModel.CurrentTamanhos);
                            addInClause("Modelo", viewModel.CurrentModelos);

                            string monitorHeader = "PartNumber,ColaboradorNome,Marca,Modelo,Tamanho";
                            csvBuilder.AppendLine(monitorHeader);
                            string whereSqlMonitores = whereClauses.Any() ? $"WHERE {string.Join(" AND ", whereClauses)}" : "";
                            sql = $"SELECT {monitorHeader} FROM Monitores {whereSqlMonitores}";
                            
                            using (var cmd = new SqlCommand(sql, connection))
                            {
                                foreach (var p in parameters)
                                {
                                    cmd.Parameters.AddWithValue(p.Key, p.Value);
                                }

                                using (var reader = cmd.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        var line = new List<string>();
                                        foreach (var col in monitorHeader.Split(','))
                                        {
                                            line.Add(reader[col].ToString());
                                        }
                                        csvBuilder.AppendLine(string.Join(",", line));
                                    }
                                }
                            }
                            break;

                        case DeviceType.Perifericos:
                            addInClause("Tipo", viewModel.CurrentTiposPeriferico);

                            string perifericoHeader = "ID,ColaboradorNome,Tipo,DataEntrega,PartNumber";
                            csvBuilder.AppendLine(perifericoHeader);
                            string whereSqlPerifericos = whereClauses.Any() ? $"WHERE {string.Join(" AND ", whereClauses)}" : "";
                            sql = $"SELECT {perifericoHeader} FROM Perifericos {whereSqlPerifericos}";
                            
                            using (var cmd = new SqlCommand(sql, connection))
                            {
                                foreach (var p in parameters)
                                {
                                    cmd.Parameters.AddWithValue(p.Key, p.Value);
                                }

                                using (var reader = cmd.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        var line = new List<string>();
                                        foreach (var col in perifericoHeader.Split(','))
                                        {
                                            line.Add(reader[col].ToString());
                                        }
                                        csvBuilder.AppendLine(string.Join(",", line));
                                    }
                                }
                            }
                            break;
                    }
                }
                else if (viewModel.ExportMode == ExportMode.PorColaborador)
                {
                    fileName = $"export_colaborador_{viewModel.ColaboradorNome}_{DateTime.Now:yyyyMMddHHmmss}.csv";
                    
                    var whereSql = "WHERE ColaboradorNome = @colaborador";
                    parameters.Add("@colaborador", viewModel.ColaboradorNome);

                    // Computadores
                    csvBuilder.AppendLine("Computadores");
                    csvBuilder.AppendLine("MAC,IP,Hostname,Fabricante,Processador,SO,DataColeta");
                    string sqlComputadores = $"SELECT MAC, IP, Hostname, Fabricante, Processador, SO, DataColeta FROM Computadores {whereSql}";
                    using (var cmd = new SqlCommand(sqlComputadores, connection))
                    {
                        foreach (var p in parameters)
                        {
                            cmd.Parameters.AddWithValue(p.Key, p.Value);
                        }
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                csvBuilder.AppendLine($"{reader["MAC"]},{reader["IP"]},{reader["Hostname"]},{reader["Fabricante"]},{reader["Processador"]},{reader["SO"]},{reader["DataColeta"]}");
                            }
                        }
                    }

                    // Monitores
                    csvBuilder.AppendLine();
                    csvBuilder.AppendLine("Monitores");
                    csvBuilder.AppendLine("PartNumber,ColaboradorNome,Marca,Modelo,Tamanho");
                    string sqlMonitores = $"SELECT PartNumber, ColaboradorNome, Marca, Modelo, Tamanho FROM Monitores {whereSql}";
                    using (var cmd = new SqlCommand(sqlMonitores, connection))
                    {
                        foreach (var p in parameters)
                        {
                            cmd.Parameters.AddWithValue(p.Key, p.Value);
                        }
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                csvBuilder.AppendLine($"{reader["PartNumber"]},{reader["ColaboradorNome"]},{reader["Marca"]},{reader["Modelo"]},{reader["Tamanho"]}");
                            }
                        }
                    }

                    // Perifericos
                    csvBuilder.AppendLine();
                    csvBuilder.AppendLine("Perifericos");
                    csvBuilder.AppendLine("ID,ColaboradorNome,Tipo,DataEntrega,PartNumber");
                    string sqlPerifericos = $"SELECT ID, ColaboradorNome, Tipo, DataEntrega, PartNumber FROM Perifericos {whereSql}";
                    using (var cmd = new SqlCommand(sqlPerifericos, connection))
                    {
                        foreach (var p in parameters)
                        {
                            cmd.Parameters.AddWithValue(p.Key, p.Value);
                        }
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                csvBuilder.AppendLine($"{reader["ID"]},{reader["ColaboradorNome"]},{reader["Tipo"]},{reader["DataEntrega"]},{reader["PartNumber"]}");
                            }
                        }
                    }
                }
                else if (viewModel.ExportMode == ExportMode.PorCoordenador)
                {
                    fileName = $"export_por_coordenador_{DateTime.Now:yyyyMMddHHmmss}.csv";
                    var selectedCoordenadores = new List<string>();

                    if (User.IsInRole("Admin") || User.IsInRole("Diretoria") || User.IsInRole("RH"))
                    {
                        selectedCoordenadores = viewModel.SelectedCoordenadores;
                    }
                    else if (User.IsInRole("Coordenador"))
                    {
                        selectedCoordenadores.Add(User.Identity.Name);
                    }

                    if (selectedCoordenadores.Any())
                    {
                        var colaboradores = new List<Colaborador>();
                        var computadores = new List<Computador>();

                        var coordenadorParams = new List<string>();
                        for (int i = 0; i < selectedCoordenadores.Count; i++)
                        {
                            coordenadorParams.Add($"@coordenador{i}");
                        }
                        var whereClauseCoordenadores = $"WHERE Coordenador IN ({string.Join(", ", coordenadorParams)})";

                        var sqlColaboradores = $"SELECT Nome, CPF, Email, Setor, Coordenador FROM Colaboradores {whereClauseCoordenadores}";
                        using (var cmd = new SqlCommand(sqlColaboradores, connection))
                        {
                            for (int i = 0; i < selectedCoordenadores.Count; i++)
                            {
                                cmd.Parameters.AddWithValue($"@coordenador{i}", selectedCoordenadores[i]);
                            }

                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    colaboradores.Add(new Colaborador
                                    {
                                        Nome = reader["Nome"].ToString(),
                                        CPF = reader["CPF"].ToString(),
                                        Email = reader["Email"].ToString(),
                                        Setor = reader["Setor"].ToString(),
                                        Coordenador = reader["Coordenador"].ToString()
                                    });
                                }
                            }
                        }

                        var collaboratorNames = colaboradores.Select(c => c.Nome).ToList();
                        if (viewModel.IncluirCoordenador)
                        {
                            collaboratorNames.AddRange(selectedCoordenadores.Where(sc => !collaboratorNames.Contains(sc)));
                        }

                        if (collaboratorNames.Any())
                        {
                            var colaboradorParams = new List<string>();
                            for (int i = 0; i < collaboratorNames.Count; i++)
                            {
                                colaboradorParams.Add($"@colaborador{i}");
                            }
                            var whereClauseComputadores = $"WHERE ColaboradorNome IN ({string.Join(", ", colaboradorParams)})";
                            var computerHeader = "MAC,IP,ColaboradorNome,Hostname,Fabricante,Processador,SO,DataColeta";
                            var sqlComputadores = $"SELECT {computerHeader} FROM Computadores {whereClauseComputadores}";

                            using (var cmd = new SqlCommand(sqlComputadores, connection))
                            {
                                for (int i = 0; i < collaboratorNames.Count; i++)
                                {
                                    cmd.Parameters.AddWithValue($"@colaborador{i}", collaboratorNames[i]);
                                }

                                using (var reader = cmd.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        computadores.Add(new Computador
                                        {
                                            MAC = reader["MAC"].ToString(),
                                            IP = reader["IP"].ToString(),
                                            ColaboradorNome = reader["ColaboradorNome"].ToString(),
                                            Hostname = reader["Hostname"].ToString(),
                                            Fabricante = reader["Fabricante"].ToString(),
                                            Processador = reader["Processador"].ToString(),
                                            SO = reader["SO"].ToString(),
                                            DataColeta = reader["DataColeta"] as DateTime?
                                        });
                                    }
                                }
                            }
                        }

                        csvBuilder.AppendLine("Colaboradores");
                        csvBuilder.AppendLine("Nome,CPF,Email,Setor,Coordenador");
                        foreach (var c in colaboradores)
                        {
                            csvBuilder.AppendLine($"{c.Nome},{c.CPF},{c.Email},{c.Setor},{c.Coordenador}");
                        }

                        csvBuilder.AppendLine();
                        csvBuilder.AppendLine("Computadores");
                        csvBuilder.AppendLine("MAC,IP,ColaboradorNome,Hostname,Fabricante,Processador,SO,DataColeta");
                        foreach (var c in computadores)
                        {
                            csvBuilder.AppendLine($"{c.MAC},{c.IP},{c.ColaboradorNome},{c.Hostname},{c.Fabricante},{c.Processador},{c.SO},{c.DataColeta}");
                        }
                    }
                }
            }

            return File(Encoding.UTF8.GetBytes(csvBuilder.ToString()), "text/csv", fileName);
        }
    }
}
