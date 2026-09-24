using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Web.Models;
using Monitor = Web.Models.Monitor;

namespace Web.Services
{
    public class MonitorService : IMonitorService
    {
        private readonly IDatabaseService _databaseService;
        private readonly ILogger<MonitorService> _logger;

        public MonitorService(IDatabaseService databaseService, ILogger<MonitorService> logger)
        {
            _databaseService = databaseService;
            _logger = logger;
        }

        public MonitorIndexViewModel GetFilteredMonitores(List<string> currentMarcas, List<string> currentTamanhos, List<string> currentModelos, string userCpf, bool isColaboradorOnly, bool isCoordenadorOnly)
        {
            var viewModel = new MonitorIndexViewModel
            {
                CurrentMarcas = currentMarcas,
                CurrentTamanhos = currentTamanhos,
                CurrentModelos = currentModelos,
                Monitores = new List<Monitor>()
            };

            using (var connection = _databaseService.CreateConnection())
            {
                connection.Open();

                viewModel.Marcas = GetDistinctMonitorValues(connection, "Marca");
                viewModel.Tamanhos = GetDistinctMonitorValues(connection, "Tamanho");
                viewModel.Modelos = GetDistinctMonitorValues(connection, "Modelo");

                var whereClauses = new List<string>();
                var parameters = new Dictionary<string, object>();

                if (isColaboradorOnly)
                {
                    whereClauses.Add("m.ColaboradorCPF = @UserCpf");
                    parameters.Add("@UserCpf", (object)userCpf ?? DBNull.Value);
                }
                else if (isCoordenadorOnly)
                {
                    whereClauses.Add("(c.CoordenadorCPF = @UserCpf OR m.ColaboradorCPF = @UserCpf)");
                    parameters.Add("@UserCpf", (object)userCpf ?? DBNull.Value);
                }

                Action<string, List<string>> addInClause = (columnName, values) =>
                {
                    if (values != null && values.Any())
                    {
                        var paramNames = new List<string>();
                        for (int i = 0; i < values.Count; i++)
                        {
                            var paramName = $"@{(columnName.Split('.').Last()).ToLower()}{i}";
                            paramNames.Add(paramName);
                            parameters.Add(paramName, values[i]);
                        }
                        whereClauses.Add($"{columnName} IN ({string.Join(", ", paramNames)})");
                    }
                };

                addInClause("m.Marca", currentMarcas);
                addInClause("m.Tamanho", currentTamanhos);
                addInClause("m.Modelo", currentModelos);

                string whereSql = whereClauses.Any() ? $"WHERE {string.Join(" AND ", whereClauses)}" : "";

                string sql = $"SELECT m.*, c.Nome as ColaboradorNome FROM Monitores m LEFT JOIN Colaboradores c ON m.ColaboradorCPF = c.CPF {whereSql}";

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
                            viewModel.Monitores.Add(new Monitor
                            {
                                PartNumber = reader["PartNumber"].ToString(),
                                ColaboradorCPF = reader["ColaboradorCPF"] != DBNull.Value ? reader["ColaboradorCPF"].ToString() : null,
                                ColaboradorNome = reader["ColaboradorNome"] != DBNull.Value ? reader["ColaboradorNome"].ToString() : null,
                                Marca = reader["Marca"].ToString(),
                                Modelo = reader["Modelo"].ToString(),
                                Tamanho = reader["Tamanho"].ToString()
                            });
                        }
                    }
                }
            }

            return viewModel;
        }

        public Monitor FindById(string partNumber)
        {
            using (var connection = _databaseService.CreateConnection())
            {
                connection.Open();
                return FindById(partNumber, connection, null);
            }
        }

        public Monitor FindById(string partNumber, IDbConnection connection, IDbTransaction transaction)
        {
            Monitor monitor = null;
            string sql = "SELECT m.*, c.Nome AS ColaboradorNome FROM Monitores m LEFT JOIN Colaboradores c ON m.ColaboradorCPF = c.CPF WHERE m.PartNumber = @PartNumber";
            using (var cmd = connection.CreateCommand())
            {
                cmd.Transaction = transaction;
                cmd.CommandText = sql;
                var p = cmd.CreateParameter(); p.ParameterName = "@PartNumber"; p.Value = partNumber; cmd.Parameters.Add(p);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        monitor = new Monitor
                        {
                            PartNumber = reader["PartNumber"].ToString(),
                            ColaboradorCPF = reader["ColaboradorCPF"] != DBNull.Value ? reader["ColaboradorCPF"].ToString() : null,
                            ColaboradorNome = reader["ColaboradorNome"] != DBNull.Value ? reader["ColaboradorNome"].ToString() : null,
                            Marca = reader["Marca"].ToString(),
                            Modelo = reader["Modelo"].ToString(),
                            Tamanho = reader["Tamanho"].ToString(),
                            DataGarantia = reader["DataGarantia"] != DBNull.Value ? Convert.ToDateTime(reader["DataGarantia"]) : (DateTime?)null
                        };
                    }
                }
            }
            return monitor;
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

        public void Create(Monitor monitor)
        {
            using (var connection = _databaseService.CreateConnection())
            {
                connection.Open();
                string sql = "INSERT INTO Monitores (PartNumber, ColaboradorCPF, Marca, Modelo, Tamanho, DataGarantia) VALUES (@PartNumber, @ColaboradorCPF, @Marca, @Modelo, @Tamanho, @DataGarantia)";
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = sql;
                    AddMonitorParameters(cmd, monitor);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Update(Monitor monitor)
        {
            using (var connection = _databaseService.CreateConnection())
            {
                connection.Open();
                string sql = "UPDATE Monitores SET ColaboradorCPF = @ColaboradorCPF, Marca = @Marca, Modelo = @Modelo, Tamanho = @Tamanho, DataGarantia = @DataGarantia WHERE PartNumber = @PartNumber";
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = sql;
                    AddMonitorParameters(cmd, monitor);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Delete(string partNumber)
        {
            using (var connection = _databaseService.CreateConnection())
            {
                connection.Open();
                string sql = "DELETE FROM Monitores WHERE PartNumber = @PartNumber";
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = sql;
                    var p = cmd.CreateParameter(); p.ParameterName = "@PartNumber"; p.Value = partNumber; cmd.Parameters.Add(p);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public (int adicionados, int atualizados, List<string> invalidCpfs) ImportList(List<Monitor> monitores)
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
                    foreach (var monitor in monitores)
                    {
                        if (!string.IsNullOrEmpty(monitor.ColaboradorCPF) && !colaboradoresCpf.Contains(monitor.ColaboradorCPF))
                        {
                            invalidCpfs.Add(monitor.PartNumber);
                            monitor.ColaboradorCPF = null;
                        }

                        var existente = FindById(monitor.PartNumber, connection, transaction);

                        if (existente != null)
                        {
                            string updateSql = @"UPDATE Monitores SET
                                               ColaboradorCPF = @ColaboradorCPF, Marca = @Marca, Modelo = @Modelo, Tamanho = @Tamanho, DataGarantia = @DataGarantia
                                               WHERE PartNumber = @PartNumber";
                            using (var cmd = connection.CreateCommand())
                            {
                                cmd.Transaction = transaction;
                                cmd.CommandText = updateSql;
                                AddMonitorParameters(cmd, monitor);
                                cmd.ExecuteNonQuery();
                            }
                            atualizados++;
                        }
                        else
                        {
                            string insertSql = @"INSERT INTO Monitores (PartNumber, ColaboradorCPF, Marca, Modelo, Tamanho, DataGarantia)
                                               VALUES (@PartNumber, @ColaboradorCPF, @Marca, @Modelo, @Tamanho, @DataGarantia)";
                            using (var cmd = connection.CreateCommand())
                            {
                                cmd.Transaction = transaction;
                                cmd.CommandText = insertSql;
                                AddMonitorParameters(cmd, monitor);
                                cmd.ExecuteNonQuery();
                            }
                            adicionados++;
                        }
                    }
                    transaction.Commit();
                }
            }

            return (adicionados, atualizados, invalidCpfs);
        }

        private void AddMonitorParameters(IDbCommand cmd, Monitor monitor)
        {
            var p1 = cmd.CreateParameter(); p1.ParameterName = "@PartNumber"; p1.Value = monitor.PartNumber; cmd.Parameters.Add(p1);
            var p2 = cmd.CreateParameter(); p2.ParameterName = "@ColaboradorCPF"; p2.Value = (object)monitor.ColaboradorCPF ?? DBNull.Value; cmd.Parameters.Add(p2);
            var p3 = cmd.CreateParameter(); p3.ParameterName = "@Marca"; p3.Value = (object)monitor.Marca ?? DBNull.Value; cmd.Parameters.Add(p3);
            var p4 = cmd.CreateParameter(); p4.ParameterName = "@Modelo"; p4.Value = (object)monitor.Modelo ?? DBNull.Value; cmd.Parameters.Add(p4);
            var p5 = cmd.CreateParameter(); p5.ParameterName = "@Tamanho"; p5.Value = (object)monitor.Tamanho ?? DBNull.Value; cmd.Parameters.Add(p5);
            var p6 = cmd.CreateParameter(); p6.ParameterName = "@DataGarantia"; p6.Value = monitor.DataGarantia.HasValue ? monitor.DataGarantia.Value.ToString("yyyy-MM-dd HH:mm:ss") : DBNull.Value; cmd.Parameters.Add(p6);
        }

        private List<string> GetDistinctMonitorValues(IDbConnection connection, string columnName)
        {
            var values = new List<string>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = $"SELECT DISTINCT {columnName} FROM Monitores WHERE {columnName} IS NOT NULL ORDER BY {columnName}";
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
    }
}
