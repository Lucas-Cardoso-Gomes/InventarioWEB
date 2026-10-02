using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Web.Models;

namespace Web.Services
{
    public class PerifericoService : IPerifericoService
    {
        private readonly IDatabaseService _databaseService;
        private readonly ILogger<PerifericoService> _logger;

        public PerifericoService(IDatabaseService databaseService, ILogger<PerifericoService> logger)
        {
            _databaseService = databaseService;
            _logger = logger;
        }

        public List<Periferico> GetFilteredPerifericos(string searchString, string userCpf, bool isColaboradorOnly, bool isCoordenadorOnly)
        {
            var perifericos = new List<Periferico>();
            using (var connection = _databaseService.CreateConnection())
            {
                connection.Open();

                var sqlBuilder = new StringBuilder("SELECT p.*, c.Nome as ColaboradorNome FROM Perifericos p LEFT JOIN Colaboradores c ON p.ColaboradorCPF = c.CPF");
                var whereClauses = new List<string>();
                var parameters = new Dictionary<string, object>();

                if (isColaboradorOnly)
                {
                    whereClauses.Add("p.ColaboradorCPF = @UserCpf");
                    parameters.Add("@UserCpf", (object)userCpf ?? DBNull.Value);
                }
                else if (isCoordenadorOnly)
                {
                    whereClauses.Add("(c.CoordenadorCPF = @UserCpf OR p.ColaboradorCPF = @UserCpf)");
                    parameters.Add("@UserCpf", (object)userCpf ?? DBNull.Value);
                }

                if (!string.IsNullOrEmpty(searchString))
                {
                    whereClauses.Add("(c.Nome LIKE @search OR p.Tipo LIKE @search OR p.PartNumber LIKE @search)");
                    parameters.Add("@search", $"%{searchString}%");
                }

                if (whereClauses.Count > 0)
                {
                    sqlBuilder.Append(" WHERE " + string.Join(" AND ", whereClauses));
                }

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = sqlBuilder.ToString();
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
                            perifericos.Add(new Periferico
                            {
                                PartNumber = reader["PartNumber"].ToString(),
                                ColaboradorCPF = reader["ColaboradorCPF"] as string,
                                ColaboradorNome = reader["ColaboradorNome"] as string,
                                Tipo = reader["Tipo"].ToString(),
                                DataEntrega = reader["DataEntrega"] != DBNull.Value ? Convert.ToDateTime(reader["DataEntrega"]) : (DateTime?)null,
                                DataGarantia = reader["DataGarantia"] != DBNull.Value ? Convert.ToDateTime(reader["DataGarantia"]) : (DateTime?)null
                            });
                        }
                    }
                }
            }
            return perifericos;
        }

        public Periferico FindById(string partNumber)
        {
            using (var connection = _databaseService.CreateConnection())
            {
                connection.Open();
                return FindById(partNumber, connection, null);
            }
        }

        public Periferico FindById(string partNumber, IDbConnection connection, IDbTransaction transaction)
        {
            Periferico periferico = null;
            string sql = "SELECT p.*, c.Nome AS ColaboradorNome FROM Perifericos p LEFT JOIN Colaboradores c ON p.ColaboradorCPF = c.CPF WHERE p.PartNumber = @PartNumber";
            using (var cmd = connection.CreateCommand())
            {
                cmd.Transaction = transaction;
                cmd.CommandText = sql;
                var p1 = cmd.CreateParameter(); p1.ParameterName = "@PartNumber"; p1.Value = partNumber; cmd.Parameters.Add(p1);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        periferico = new Periferico
                        {
                            PartNumber = reader["PartNumber"].ToString(),
                            ColaboradorCPF = reader["ColaboradorCPF"] as string,
                            ColaboradorNome = reader["ColaboradorNome"] as string,
                            Tipo = reader["Tipo"].ToString(),
                            DataEntrega = reader["DataEntrega"] != DBNull.Value ? Convert.ToDateTime(reader["DataEntrega"]) : (DateTime?)null,
                            DataGarantia = reader["DataGarantia"] != DBNull.Value ? Convert.ToDateTime(reader["DataGarantia"]) : (DateTime?)null
                        };
                    }
                }
            }
            return periferico;
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

        public void Create(Periferico periferico)
        {
            using (var connection = _databaseService.CreateConnection())
            {
                connection.Open();
                string sql = "INSERT INTO Perifericos (PartNumber, ColaboradorCPF, Tipo, DataEntrega, DataGarantia) VALUES (@PartNumber, @ColaboradorCPF, @Tipo, @DataEntrega, @DataGarantia)";
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = sql;
                    AddPerifericoParameters(cmd, periferico);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Update(Periferico periferico)
        {
            using (var connection = _databaseService.CreateConnection())
            {
                connection.Open();
                string sql = "UPDATE Perifericos SET ColaboradorCPF = @ColaboradorCPF, Tipo = @Tipo, DataEntrega = @DataEntrega, DataGarantia = @DataGarantia WHERE PartNumber = @PartNumber";
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = sql;
                    AddPerifericoParameters(cmd, periferico);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void Delete(string partNumber)
        {
            using (var connection = _databaseService.CreateConnection())
            {
                connection.Open();
                string sql = "DELETE FROM Perifericos WHERE PartNumber = @PartNumber";
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = sql;
                    var p1 = cmd.CreateParameter(); p1.ParameterName = "@PartNumber"; p1.Value = partNumber; cmd.Parameters.Add(p1);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public (int adicionados, int atualizados, List<string> invalidCpfs) ImportList(List<Periferico> perifericos)
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
                    foreach (var periferico in perifericos)
                    {
                        if (!string.IsNullOrEmpty(periferico.ColaboradorCPF) && !colaboradoresCpf.Contains(periferico.ColaboradorCPF))
                        {
                            invalidCpfs.Add(periferico.PartNumber);
                            periferico.ColaboradorCPF = null;
                        }

                        var existente = FindById(periferico.PartNumber, connection, transaction);
                        if (existente != null)
                        {
                            string updateSql = @"UPDATE Perifericos SET
                                               ColaboradorCPF = @ColaboradorCPF, Tipo = @Tipo, DataEntrega = @DataEntrega, DataGarantia = @DataGarantia
                                               WHERE PartNumber = @PartNumber";
                            using (var cmd = connection.CreateCommand())
                            {
                                cmd.Transaction = transaction;
                                cmd.CommandText = updateSql;
                                AddPerifericoParameters(cmd, periferico);
                                cmd.ExecuteNonQuery();
                            }
                            atualizados++;
                        }
                        else
                        {
                            string insertSql = @"INSERT INTO Perifericos (PartNumber, ColaboradorCPF, Tipo, DataEntrega, DataGarantia)
                                               VALUES (@PartNumber, @ColaboradorCPF, @Tipo, @DataEntrega, @DataGarantia)";
                            using (var cmd = connection.CreateCommand())
                            {
                                cmd.Transaction = transaction;
                                cmd.CommandText = insertSql;
                                AddPerifericoParameters(cmd, periferico);
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

        private void AddPerifericoParameters(IDbCommand cmd, Periferico periferico)
        {
            var p1 = cmd.CreateParameter(); p1.ParameterName = "@PartNumber"; p1.Value = periferico.PartNumber; cmd.Parameters.Add(p1);
            var p2 = cmd.CreateParameter(); p2.ParameterName = "@ColaboradorCPF"; p2.Value = (object)periferico.ColaboradorCPF ?? DBNull.Value; cmd.Parameters.Add(p2);
            var p3 = cmd.CreateParameter(); p3.ParameterName = "@Tipo"; p3.Value = periferico.Tipo; cmd.Parameters.Add(p3);
            var p4 = cmd.CreateParameter(); p4.ParameterName = "@DataEntrega"; p4.Value = (object)periferico.DataEntrega ?? DBNull.Value; cmd.Parameters.Add(p4);
            var p5 = cmd.CreateParameter(); p5.ParameterName = "@DataGarantia"; p5.Value = periferico.DataGarantia.HasValue ? periferico.DataGarantia.Value.ToString("yyyy-MM-dd HH:mm:ss") : DBNull.Value; cmd.Parameters.Add(p5);
        }
    }
}
