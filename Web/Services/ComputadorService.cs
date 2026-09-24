using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Web.Models;

namespace Web.Services
{
    public class ComputadorService : IComputadorService
    {
        private readonly IDatabaseService _databaseService;
        private readonly ILogger<ComputadorService> _logger;

        public ComputadorService(IDatabaseService databaseService, ILogger<ComputadorService> logger)
        {
            _databaseService = databaseService;
            _logger = logger;
        }

        public ComputadorIndexViewModel GetPagedComputadores(string sortOrder, string searchString,
            List<string> currentFabricantes, List<string> currentSOs, List<string> currentProcessadorFabricantes,
            List<string> currentRamTipos, List<string> currentProcessadores, List<string> currentRams,
            string userCpf, bool isColaboradorOnly, bool isCoordenadorOnly,
            int pageNumber, int pageSize)
        {
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

                string baseSql = @"
                    FROM Computadores comp
                    LEFT JOIN Colaboradores col ON comp.ColaboradorCPF = col.CPF
                    LEFT JOIN Processadores p ON comp.ProcessadorId = p.Id
                ";

                if (isColaboradorOnly)
                {
                    whereClauses.Add("comp.ColaboradorCPF = @UserCpf");
                    parameters.Add("@UserCpf", (object)userCpf ?? DBNull.Value);
                }
                else if (isCoordenadorOnly)
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
                addInClause("COALESCE(p.Fabricante, comp.ProcessadorFabricante)", currentProcessadorFabricantes);
                addInClause("comp.RamTipo", currentRamTipos);
                addInClause("COALESCE(p.Nome, comp.Processador)", currentProcessadores);
                addInClause("comp.Ram", currentRams);

                string whereSql = whereClauses.Any() ? $"WHERE {string.Join(" AND ", whereClauses)}" : "";

                string countSql = $"SELECT COUNT(comp.MAC) {baseSql} {whereSql}";
                using (var countCommand = connection.CreateCommand())
                {
                    countCommand.CommandText = countSql;
                    foreach (var p in parameters)
                    {
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

                string sqlFields = @"
                    SELECT
                        comp.MAC, comp.IP, comp.ColaboradorCPF, col.Nome as ColaboradorNome, comp.Hostname,
                        comp.Fabricante, COALESCE(p.Nome, comp.Processador) AS Processador,
                        COALESCE(p.Fabricante, comp.ProcessadorFabricante) AS ProcessadorFabricante,
                        COALESCE(p.Cores, comp.ProcessadorCore) AS ProcessadorCore,
                        COALESCE(p.Threads, comp.ProcessadorThread) AS ProcessadorThread,
                        COALESCE(p.Clock, comp.ProcessadorClock) AS ProcessadorClock,
                        comp.ProcessadorId, comp.Ram, comp.RamTipo,
                        comp.RamVelocidade, comp.RamVoltagem, comp.RamPorModule, comp.ConsumoCPU, comp.SO,
                        comp.DataColeta, comp.PartNumber, comp.ProcessadorTemperatura, comp.DataGarantia,
                        comp.BateriaWearLevel, comp.TempoAtividade, comp.Localizacao, comp.Backup
                ";
                string sql = $"{sqlFields} {baseSql} {whereSql} {orderBySql} LIMIT @pageSize OFFSET @offset";

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
                                ProcessadorId = reader["ProcessadorId"] != DBNull.Value ? Convert.ToInt32(reader["ProcessadorId"]) : null,
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
                                ConsumoCPU = reader["ConsumoCPU"].ToString(),
                                SO = reader["SO"].ToString(),
                                DataColeta = reader["DataColeta"] != DBNull.Value ? Convert.ToDateTime(reader["DataColeta"]) : (DateTime?)null,
                                PartNumber = reader["PartNumber"].ToString(),
                                ProcessadorTemperatura = reader["ProcessadorTemperatura"].ToString(),
                                DataGarantia = reader["DataGarantia"] != DBNull.Value ? Convert.ToDateTime(reader["DataGarantia"]) : (DateTime?)null,
                                BateriaWearLevel = reader["BateriaWearLevel"] != DBNull.Value ? reader["BateriaWearLevel"].ToString() : null,
                                TempoAtividade = reader["TempoAtividade"] != DBNull.Value ? reader["TempoAtividade"].ToString() : null,
                                Localizacao = reader["Localizacao"] != DBNull.Value ? reader["Localizacao"].ToString() : null,
                                Backup = reader["Backup"] != DBNull.Value ? reader["Backup"].ToString() : null
                            });
                        }
                    }
                }
            }

            return viewModel;
        }

        public Computador FindById(string mac)
        {
            using (var connection = _databaseService.CreateConnection())
            {
                connection.Open();
                var comp = FindById(mac, connection, null);
                if (comp != null)
                {
                    CarregarDiscos(connection, null, comp);
                }
                return comp;
            }
        }

        public Computador FindById(string mac, IDbConnection connection, IDbTransaction transaction)
        {
            Computador computador = null;
            string sql = @"
                SELECT comp.*, col.Nome AS ColaboradorNome,
                       COALESCE(p.Nome, comp.Processador) AS Processador,
                       COALESCE(p.Fabricante, comp.ProcessadorFabricante) AS ProcessadorFabricante,
                       COALESCE(p.Cores, comp.ProcessadorCore) AS ProcessadorCore,
                       COALESCE(p.Threads, comp.ProcessadorThread) AS ProcessadorThread,
                       COALESCE(p.Clock, comp.ProcessadorClock) AS ProcessadorClock
                FROM Computadores comp 
                LEFT JOIN Colaboradores col ON comp.ColaboradorCPF = col.CPF
                LEFT JOIN Processadores p ON comp.ProcessadorId = p.Id 
                WHERE comp.MAC = @MAC";
            using (var cmd = connection.CreateCommand())
            {
                cmd.Transaction = transaction;
                cmd.CommandText = sql;
                var p1 = cmd.CreateParameter(); p1.ParameterName = "@MAC"; p1.Value = mac; cmd.Parameters.Add(p1);
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
                            ProcessadorId = reader["ProcessadorId"] != DBNull.Value ? Convert.ToInt32(reader["ProcessadorId"]) : null,
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
                            ConsumoCPU = reader["ConsumoCPU"].ToString(),
                            SO = reader["SO"].ToString(),
                            DataColeta = reader["DataColeta"] != DBNull.Value ? Convert.ToDateTime(reader["DataColeta"]) : (DateTime?)null,
                            PartNumber = reader["PartNumber"].ToString(),
                            DataGarantia = reader["DataGarantia"] != DBNull.Value ? Convert.ToDateTime(reader["DataGarantia"]) : (DateTime?)null,
                            BateriaWearLevel = reader["BateriaWearLevel"] != DBNull.Value ? reader["BateriaWearLevel"].ToString() : null,
                            TempoAtividade = reader["TempoAtividade"] != DBNull.Value ? reader["TempoAtividade"].ToString() : null,
                            Localizacao = reader["Localizacao"] != DBNull.Value ? reader["Localizacao"].ToString() : null,
                            Backup = reader["Backup"].ToString(),
                            ProcessadorTemperatura = reader["ProcessadorTemperatura"].ToString()
                        };
                    }
                }
            }
            return computador;
        }

        public List<Colaborador> GetColaboradores()
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
                            colaboradores.Add(new Colaborador
                            {
                                CPF = reader["CPF"].ToString(),
                                Nome = reader["Nome"].ToString()
                            });
                        }
                    }
                }
            }
            return colaboradores;
        }

        public void Create(Computador comp)
        {
            using (var connection = _databaseService.CreateConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    comp.ProcessadorId = EnsureProcessadorId(connection, transaction, comp.Processador, comp.ProcessadorFabricante, comp.ProcessadorCore, comp.ProcessadorThread, comp.ProcessadorClock);
                    string sql = "INSERT INTO Computadores (MAC, IP, ColaboradorCPF, Hostname, Fabricante, Processador, ProcessadorFabricante, ProcessadorCore, ProcessadorThread, ProcessadorClock, ProcessadorId, ProcessadorTemperatura, Ram, RamTipo, RamVelocidade, RamVoltagem, RamPorModule, ConsumoCPU, SO, DataColeta, PartNumber, DataGarantia, Backup, BateriaWearLevel, TempoAtividade, Localizacao) VALUES (@MAC, @IP, @ColaboradorCPF, @Hostname, @Fabricante, @Processador, @ProcessadorFabricante, @ProcessadorCore, @ProcessadorThread, @ProcessadorClock, @ProcessadorId, @ProcessadorTemperatura, @Ram, @RamTipo, @RamVelocidade, @RamVoltagem, @RamPorModule, @ConsumoCPU, @SO, @DataColeta, @PartNumber, @DataGarantia, @Backup, @BateriaWearLevel, @TempoAtividade, @Localizacao)";
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.Transaction = transaction;
                        cmd.CommandText = sql;
                        AddComputadorParameters(cmd, comp);
                        var pDate = cmd.CreateParameter(); pDate.ParameterName = "@DataColeta"; pDate.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); cmd.Parameters.Add(pDate);
                        cmd.ExecuteNonQuery();
                    }
                    SalvarDiscos(connection, transaction, comp);
                    transaction.Commit();
                }
            }
        }

        public void Update(Computador comp)
        {
            using (var connection = _databaseService.CreateConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    comp.ProcessadorId = EnsureProcessadorId(connection, transaction, comp.Processador, comp.ProcessadorFabricante, comp.ProcessadorCore, comp.ProcessadorThread, comp.ProcessadorClock);
                    string sql = "UPDATE Computadores SET IP = @IP, ColaboradorCPF = @ColaboradorCPF, Hostname = @Hostname, Fabricante = @Fabricante, Processador = @Processador, ProcessadorFabricante = @ProcessadorFabricante, ProcessadorCore = @ProcessadorCore, ProcessadorThread = @ProcessadorThread, ProcessadorClock = @ProcessadorClock, ProcessadorId = @ProcessadorId, Ram = @Ram, RamTipo = @RamTipo, RamVelocidade = @RamVelocidade, RamVoltagem = @RamVoltagem, RamPorModule = @RamPorModule, ConsumoCPU = @ConsumoCPU, SO = @SO, PartNumber = @PartNumber, DataGarantia = @DataGarantia, Backup = @Backup, BateriaWearLevel = @BateriaWearLevel, TempoAtividade = @TempoAtividade, Localizacao = @Localizacao WHERE MAC = @MAC";
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.Transaction = transaction;
                        cmd.CommandText = sql;
                        AddComputadorParameters(cmd, comp);
                        cmd.ExecuteNonQuery();
                    }
                    SalvarDiscos(connection, transaction, comp);
                    transaction.Commit();
                }
            }
        }

        public void Delete(string mac)
        {
            using (var connection = _databaseService.CreateConnection())
            {
                connection.Open();
                string sql = "DELETE FROM Computadores WHERE MAC = @MAC";
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = sql;
                    var p1 = cmd.CreateParameter(); p1.ParameterName = "@MAC"; p1.Value = mac; cmd.Parameters.Add(p1);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public (int adicionados, int atualizados, List<string> invalidCpfs) ImportList(List<Computador> computadores)
        {
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
                    foreach (var computador in computadores)
                    {
                        if (!string.IsNullOrEmpty(computador.ColaboradorCPF) && !colaboradoresCpf.Contains(computador.ColaboradorCPF))
                        {
                            invalidCpfs.Add(computador.MAC);
                            computador.ColaboradorCPF = null;
                        }

                        computador.ProcessadorId = EnsureProcessadorId(connection, transaction, computador.Processador, computador.ProcessadorFabricante, computador.ProcessadorCore, computador.ProcessadorThread, computador.ProcessadorClock);

                        var existente = FindById(computador.MAC, connection, transaction);
                        if (existente != null)
                        {
                            string updateSql = @"UPDATE Computadores SET
                                               IP = @IP, ColaboradorCPF = @ColaboradorCPF, Hostname = @Hostname, Fabricante = @Fabricante,
                                               Processador = @Processador, ProcessadorFabricante = @ProcessadorFabricante, ProcessadorCore = @ProcessadorCore,
                                               ProcessadorThread = @ProcessadorThread, ProcessadorClock = @ProcessadorClock, ProcessadorId = @ProcessadorId, Ram = @Ram,
                                               RamTipo = @RamTipo, RamVelocidade = @RamVelocidade, RamVoltagem = @RamVoltagem,
                                               RamPorModule = @RamPorModule, ConsumoCPU = @ConsumoCPU, SO = @SO, DataColeta = @DataColeta, PartNumber = @PartNumber,
                                               DataGarantia = @DataGarantia, Backup = @Backup, ProcessadorTemperatura = @ProcessadorTemperatura, BateriaWearLevel = @BateriaWearLevel, TempoAtividade = @TempoAtividade, Localizacao = @Localizacao
                                               WHERE MAC = @MAC";
                            using (var cmd = connection.CreateCommand())
                            {
                                cmd.Transaction = transaction;
                                cmd.CommandText = updateSql;
                                AddComputadorParameters(cmd, computador);
                                var pDate = cmd.CreateParameter(); pDate.ParameterName = "@DataColeta"; pDate.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); cmd.Parameters.Add(pDate);
                                cmd.ExecuteNonQuery();
                            }
                            SalvarDiscos(connection, transaction, computador);
                            atualizados++;
                        }
                        else
                        {
                            string insertSql = @"INSERT INTO Computadores (MAC, IP, ColaboradorCPF, Hostname, Fabricante, Processador, ProcessadorFabricante, ProcessadorCore, ProcessadorThread, ProcessadorClock, ProcessadorId, ProcessadorTemperatura, Ram, RamTipo, RamVelocidade, RamVoltagem, RamPorModule, ConsumoCPU, SO, DataColeta, PartNumber, DataGarantia, Backup, BateriaWearLevel, TempoAtividade, Localizacao)
                            VALUES (@MAC, @IP, @ColaboradorCPF, @Hostname, @Fabricante, @Processador, @ProcessadorFabricante, @ProcessadorCore, @ProcessadorThread, @ProcessadorClock, @ProcessadorId, @ProcessadorTemperatura, @Ram, @RamTipo, @RamVelocidade, @RamVoltagem, @RamPorModule, @ConsumoCPU, @SO, @DataColeta, @PartNumber, @DataGarantia, @Backup, @BateriaWearLevel, @TempoAtividade, @Localizacao)";
                            using (var cmd = connection.CreateCommand())
                            {
                                cmd.Transaction = transaction;
                                cmd.CommandText = insertSql;
                                AddComputadorParameters(cmd, computador);
                                var pDate = cmd.CreateParameter(); pDate.ParameterName = "@DataColeta"; pDate.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); cmd.Parameters.Add(pDate);
                                cmd.ExecuteNonQuery();
                            }
                            SalvarDiscos(connection, transaction, computador);
                            adicionados++;
                        }
                    }
                    transaction.Commit();
                }
            }

            return (adicionados, atualizados, invalidCpfs);
        }

        private List<string> GetDistinctComputerValues(IDbConnection connection, string columnName)
        {
            var values = new List<string>();
            using (var command = connection.CreateCommand())
            {
                if (columnName.Equals("Processador", StringComparison.OrdinalIgnoreCase))
                {
                    command.CommandText = "SELECT DISTINCT COALESCE(p.Nome, comp.Processador) FROM Computadores comp LEFT JOIN Processadores p ON comp.ProcessadorId = p.Id WHERE COALESCE(p.Nome, comp.Processador) IS NOT NULL AND TRIM(COALESCE(p.Nome, comp.Processador)) != '' ORDER BY 1";
                }
                else if (columnName.Equals("ProcessadorFabricante", StringComparison.OrdinalIgnoreCase))
                {
                    command.CommandText = "SELECT DISTINCT COALESCE(p.Fabricante, comp.ProcessadorFabricante) FROM Computadores comp LEFT JOIN Processadores p ON comp.ProcessadorId = p.Id WHERE COALESCE(p.Fabricante, comp.ProcessadorFabricante) IS NOT NULL AND TRIM(COALESCE(p.Fabricante, comp.ProcessadorFabricante)) != '' ORDER BY 1";
                }
                else
                {
                    command.CommandText = $"SELECT DISTINCT {columnName} FROM Computadores WHERE {columnName} IS NOT NULL ORDER BY {columnName}";
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

        private void CarregarDiscos(IDbConnection connection, IDbTransaction transaction, Computador comp)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.Transaction = transaction;
                cmd.CommandText = "SELECT Letra, TotalGB, LivreGB FROM ComputadorDiscos WHERE ComputadorMAC = @MAC";
                var p = cmd.CreateParameter(); p.ParameterName = "@MAC"; p.Value = comp.MAC; cmd.Parameters.Add(p);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string letra = reader["Letra"].ToString();
                        string total = reader["TotalGB"] != DBNull.Value ? reader["TotalGB"].ToString() : null;
                        string livre = reader["LivreGB"] != DBNull.Value ? reader["LivreGB"].ToString() : null;

                        if (letra.StartsWith("C", StringComparison.OrdinalIgnoreCase))
                        {
                            comp.ArmazenamentoC = letra;
                            comp.ArmazenamentoCTotal = total;
                            comp.ArmazenamentoCLivre = livre;
                        }
                        else if (letra.StartsWith("D", StringComparison.OrdinalIgnoreCase))
                        {
                            comp.ArmazenamentoD = letra;
                            comp.ArmazenamentoDTotal = total;
                            comp.ArmazenamentoDLivre = livre;
                        }
                    }
                }
            }
        }

        private void SalvarDiscos(IDbConnection connection, IDbTransaction transaction, Computador comp)
        {
            using (var delCmd = connection.CreateCommand())
            {
                delCmd.Transaction = transaction;
                delCmd.CommandText = "DELETE FROM ComputadorDiscos WHERE ComputadorMAC = @MAC";
                var p = delCmd.CreateParameter(); p.ParameterName = "@MAC"; p.Value = comp.MAC; delCmd.Parameters.Add(p);
                delCmd.ExecuteNonQuery();
            }

            var discos = new (string Letra, string? Total, string? Livre)[]
            {
                (comp.ArmazenamentoC ?? "C:", comp.ArmazenamentoCTotal, comp.ArmazenamentoCLivre),
                (comp.ArmazenamentoD ?? "D:", comp.ArmazenamentoDTotal, comp.ArmazenamentoDLivre)
            };

            foreach (var (letra, total, livre) in discos)
            {
                if (!string.IsNullOrWhiteSpace(total) || !string.IsNullOrWhiteSpace(livre))
                {
                    using (var insCmd = connection.CreateCommand())
                    {
                        insCmd.Transaction = transaction;
                        insCmd.CommandText = "INSERT INTO ComputadorDiscos (ComputadorMAC, Letra, TotalGB, LivreGB) VALUES (@MAC, @Letra, @TotalGB, @LivreGB)";
                        var p1 = insCmd.CreateParameter(); p1.ParameterName = "@MAC"; p1.Value = comp.MAC; insCmd.Parameters.Add(p1);
                        var p2 = insCmd.CreateParameter(); p2.ParameterName = "@Letra"; p2.Value = letra; insCmd.Parameters.Add(p2);
                        var p3 = insCmd.CreateParameter(); p3.ParameterName = "@TotalGB"; p3.Value = (object)total ?? DBNull.Value; insCmd.Parameters.Add(p3);
                        var p4 = insCmd.CreateParameter(); p4.ParameterName = "@LivreGB"; p4.Value = (object)livre ?? DBNull.Value; insCmd.Parameters.Add(p4);
                        insCmd.ExecuteNonQuery();
                    }
                }
            }
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
            var pProcId = cmd.CreateParameter(); pProcId.ParameterName = "@ProcessadorId"; pProcId.Value = (object)computador.ProcessadorId ?? DBNull.Value; cmd.Parameters.Add(pProcId);
            var p11 = cmd.CreateParameter(); p11.ParameterName = "@Ram"; p11.Value = (object)computador.Ram ?? DBNull.Value; cmd.Parameters.Add(p11);
            var p12 = cmd.CreateParameter(); p12.ParameterName = "@RamTipo"; p12.Value = (object)computador.RamTipo ?? DBNull.Value; cmd.Parameters.Add(p12);
            var p13 = cmd.CreateParameter(); p13.ParameterName = "@RamVelocidade"; p13.Value = (object)computador.RamVelocidade ?? DBNull.Value; cmd.Parameters.Add(p13);
            var p14 = cmd.CreateParameter(); p14.ParameterName = "@RamVoltagem"; p14.Value = (object)computador.RamVoltagem ?? DBNull.Value; cmd.Parameters.Add(p14);
            var p15 = cmd.CreateParameter(); p15.ParameterName = "@RamPorModule"; p15.Value = (object)computador.RamPorModule ?? DBNull.Value; cmd.Parameters.Add(p15);
            var p22 = cmd.CreateParameter(); p22.ParameterName = "@ConsumoCPU"; p22.Value = (object)computador.ConsumoCPU ?? DBNull.Value; cmd.Parameters.Add(p22);
            var p23 = cmd.CreateParameter(); p23.ParameterName = "@SO"; p23.Value = (object)computador.SO ?? DBNull.Value; cmd.Parameters.Add(p23);
            var p24 = cmd.CreateParameter(); p24.ParameterName = "@PartNumber"; p24.Value = (object)computador.PartNumber ?? DBNull.Value; cmd.Parameters.Add(p24);
            var p25 = cmd.CreateParameter(); p25.ParameterName = "@DataGarantia"; p25.Value = computador.DataGarantia.HasValue ? computador.DataGarantia.Value.ToString("yyyy-MM-dd HH:mm:ss") : DBNull.Value; cmd.Parameters.Add(p25);
            var p26 = cmd.CreateParameter(); p26.ParameterName = "@Backup"; p26.Value = (object)computador.Backup ?? DBNull.Value; cmd.Parameters.Add(p26);
            var p27 = cmd.CreateParameter(); p27.ParameterName = "@ProcessadorTemperatura"; p27.Value = (object)computador.ProcessadorTemperatura ?? DBNull.Value; cmd.Parameters.Add(p27);
            var p28 = cmd.CreateParameter(); p28.ParameterName = "@BateriaWearLevel"; p28.Value = (object)computador.BateriaWearLevel ?? DBNull.Value; cmd.Parameters.Add(p28);
            var p29 = cmd.CreateParameter(); p29.ParameterName = "@TempoAtividade"; p29.Value = (object)computador.TempoAtividade ?? DBNull.Value; cmd.Parameters.Add(p29);
            var p30 = cmd.CreateParameter(); p30.ParameterName = "@Localizacao"; p30.Value = (object)computador.Localizacao ?? DBNull.Value; cmd.Parameters.Add(p30);
        }

        private int? EnsureProcessadorId(IDbConnection connection, IDbTransaction transaction, string nome, string fabricante, string cores, string threads, string clock)
        {
            if (string.IsNullOrWhiteSpace(nome) && string.IsNullOrWhiteSpace(fabricante))
                return null;

            string procNome = string.IsNullOrWhiteSpace(nome) ? "Desconhecido" : nome.Trim();
            string procFab = string.IsNullOrWhiteSpace(fabricante) ? null : fabricante.Trim();
            string procCore = string.IsNullOrWhiteSpace(cores) ? null : cores.Trim();
            string procThread = string.IsNullOrWhiteSpace(threads) ? null : threads.Trim();
            string procClock = string.IsNullOrWhiteSpace(clock) ? null : clock.Trim();

            using (var procCmd = connection.CreateCommand())
            {
                procCmd.Transaction = transaction;
                procCmd.CommandText = @"
                    INSERT OR IGNORE INTO Processadores (Nome, Fabricante, Cores, Threads, Clock)
                    VALUES (@Nome, @Fabricante, @Cores, @Threads, @Clock);
                    
                    SELECT Id FROM Processadores 
                    WHERE Nome = @Nome 
                      AND (Fabricante = @Fabricante OR (Fabricante IS NULL AND @Fabricante IS NULL))
                      AND (Cores = @Cores OR (Cores IS NULL AND @Cores IS NULL))
                      AND (Threads = @Threads OR (Threads IS NULL AND @Threads IS NULL))
                      AND (Clock = @Clock OR (Clock IS NULL AND @Clock IS NULL))
                    LIMIT 1;";

                var pName = procCmd.CreateParameter(); pName.ParameterName = "@Nome"; pName.Value = procNome; procCmd.Parameters.Add(pName);
                var pFab = procCmd.CreateParameter(); pFab.ParameterName = "@Fabricante"; pFab.Value = (object)procFab ?? DBNull.Value; procCmd.Parameters.Add(pFab);
                var pCore = procCmd.CreateParameter(); pCore.ParameterName = "@Cores"; pCore.Value = (object)procCore ?? DBNull.Value; procCmd.Parameters.Add(pCore);
                var pThread = procCmd.CreateParameter(); pThread.ParameterName = "@Threads"; pThread.Value = (object)procThread ?? DBNull.Value; procCmd.Parameters.Add(pThread);
                var pClock = procCmd.CreateParameter(); pClock.ParameterName = "@Clock"; pClock.Value = (object)procClock ?? DBNull.Value; procCmd.Parameters.Add(pClock);

                var res = procCmd.ExecuteScalar();
                if (res != null && res != DBNull.Value)
                {
                    return Convert.ToInt32(res);
                }
            }
            return null;
        }
    }
}
