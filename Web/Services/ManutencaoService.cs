using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Web.Models;
using System.Data;

namespace Web.Services
{
    public class ManutencaoService
    {
        private readonly IDatabaseService _databaseService;

        public ManutencaoService(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public List<Manutencao> GetAllManutencoes(string partNumber, string colaborador, string hostname)
        {
            var manutencoes = new List<Manutencao>();
            using (var connection = _databaseService.CreateConnection())
            {
                connection.Open();
                var sql = @"
                    SELECT
                        m.Id, m.DataManutencaoHardware, m.DataManutencaoSoftware, m.ManutencaoExterna, m.Data, m.Historico,
                        c.MAC, c.Hostname,
                        mo.PartNumber as MonitorPN, mo.Modelo,
                        p.PartNumber as PerifericoPN, p.Tipo
                    FROM Manutencoes m
                    LEFT JOIN Computadores c ON m.ComputadorMAC = c.MAC
                    LEFT JOIN Monitores mo ON m.MonitorPartNumber = mo.PartNumber
                    LEFT JOIN Perifericos p ON m.PerifericoPartNumber = p.PartNumber
                    LEFT JOIN Colaboradores col_c ON c.ColaboradorCPF = col_c.CPF
                    LEFT JOIN Colaboradores col_mo ON mo.ColaboradorCPF = col_mo.CPF
                    LEFT JOIN Colaboradores col_p ON p.ColaboradorCPF = col_p.CPF
                    WHERE 1=1";

                var parameters = new List<Action<IDbCommand>>();

                if (!string.IsNullOrEmpty(partNumber))
                {
                    sql += " AND (c.MAC LIKE @PartNumber OR mo.PartNumber LIKE @PartNumber OR p.PartNumber LIKE @PartNumber)";
                    parameters.Add(cmd => { var p = cmd.CreateParameter(); p.ParameterName = "@PartNumber"; p.Value = $"%{partNumber}%"; cmd.Parameters.Add(p); });
                }
                if (!string.IsNullOrEmpty(colaborador))
                {
                    sql += " AND (col_c.Nome LIKE @Colaborador OR col_mo.Nome LIKE @Colaborador OR col_p.Nome LIKE @Colaborador)";
                    parameters.Add(cmd => { var p = cmd.CreateParameter(); p.ParameterName = "@Colaborador"; p.Value = $"%{colaborador}%"; cmd.Parameters.Add(p); });
                }
                if (!string.IsNullOrEmpty(hostname))
                {
                    sql += " AND c.Hostname LIKE @Hostname";
                    parameters.Add(cmd => { var p = cmd.CreateParameter(); p.ParameterName = "@Hostname"; p.Value = $"%{hostname}%"; cmd.Parameters.Add(p); });
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    foreach (var action in parameters) action(command);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var manutencao = new Manutencao
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                DataManutencaoHardware = reader["DataManutencaoHardware"] != DBNull.Value ? Convert.ToDateTime(reader["DataManutencaoHardware"]) : (DateTime?)null,
                                DataManutencaoSoftware = reader["DataManutencaoSoftware"] != DBNull.Value ? Convert.ToDateTime(reader["DataManutencaoSoftware"]) : (DateTime?)null,
                                ManutencaoExterna = reader["ManutencaoExterna"] != DBNull.Value ? reader["ManutencaoExterna"].ToString() : null,
                                Data = reader["Data"] != DBNull.Value ? Convert.ToDateTime(reader["Data"]) : (DateTime?)null,
                                Historico = reader["Historico"] != DBNull.Value ? reader["Historico"].ToString() : null,
                                ComputadorMAC = reader["MAC"] != DBNull.Value ? reader["MAC"].ToString() : null,
                                MonitorPartNumber = reader["MonitorPN"] != DBNull.Value ? reader["MonitorPN"].ToString() : null,
                                PerifericoPartNumber = reader["PerifericoPN"] != DBNull.Value ? reader["PerifericoPN"].ToString() : null
                            };

                            if (manutencao.ComputadorMAC != null)
                            {
                                manutencao.Computador = new Computador { MAC = manutencao.ComputadorMAC, Hostname = reader["Hostname"].ToString() };
                            }
                            if (manutencao.MonitorPartNumber != null)
                            {
                                manutencao.Monitor = new Web.Models.Monitor { PartNumber = manutencao.MonitorPartNumber, Modelo = reader["Modelo"].ToString() };
                            }
                            if (manutencao.PerifericoPartNumber != null)
                            {
                                manutencao.Periferico = new Periferico { PartNumber = manutencao.PerifericoPartNumber, Tipo = reader["Tipo"].ToString() };
                            }
                            manutencoes.Add(manutencao);
                        }
                    }
                }
            }
            return manutencoes;
        }

        public void AddManutencao(Manutencao manutencao)
        {
            using (var connection = _databaseService.CreateConnection())
            {
                connection.Open();
                string sql = "INSERT INTO Manutencoes (ComputadorMAC, MonitorPartNumber, PerifericoPartNumber, DataManutencaoHardware, DataManutencaoSoftware, ManutencaoExterna, Data, Historico) VALUES (@ComputadorMAC, @MonitorPartNumber, @PerifericoPartNumber, @DataManutencaoHardware, @DataManutencaoSoftware, @ManutencaoExterna, @Data, @Historico)";
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    var p1 = command.CreateParameter(); p1.ParameterName = "@ComputadorMAC"; p1.Value = (object)manutencao.ComputadorMAC ?? DBNull.Value; command.Parameters.Add(p1);
                    var p2 = command.CreateParameter(); p2.ParameterName = "@MonitorPartNumber"; p2.Value = (object)manutencao.MonitorPartNumber ?? DBNull.Value; command.Parameters.Add(p2);
                    var p3 = command.CreateParameter(); p3.ParameterName = "@PerifericoPartNumber"; p3.Value = (object)manutencao.PerifericoPartNumber ?? DBNull.Value; command.Parameters.Add(p3);
                    var p4 = command.CreateParameter(); p4.ParameterName = "@DataManutencaoHardware"; p4.Value = (object)manutencao.DataManutencaoHardware ?? DBNull.Value; command.Parameters.Add(p4);
                    var p5 = command.CreateParameter(); p5.ParameterName = "@DataManutencaoSoftware"; p5.Value = (object)manutencao.DataManutencaoSoftware ?? DBNull.Value; command.Parameters.Add(p5);
                    var p6 = command.CreateParameter(); p6.ParameterName = "@ManutencaoExterna"; p6.Value = (object)manutencao.ManutencaoExterna ?? DBNull.Value; command.Parameters.Add(p6);
                    var p7 = command.CreateParameter(); p7.ParameterName = "@Data"; p7.Value = (object)manutencao.Data ?? DBNull.Value; command.Parameters.Add(p7);
                    var p8 = command.CreateParameter(); p8.ParameterName = "@Historico"; p8.Value = (object)manutencao.Historico ?? DBNull.Value; command.Parameters.Add(p8);

                    command.ExecuteNonQuery();
                }
            }
        }

        public Manutencao GetManutencaoById(int id)
        {
            Manutencao manutencao = null;
            using (var connection = _databaseService.CreateConnection())
            {
                connection.Open();
                string sql = "SELECT Id, ComputadorMAC, MonitorPartNumber, PerifericoPartNumber, DataManutencaoHardware, DataManutencaoSoftware, ManutencaoExterna, Data, Historico FROM Manutencoes WHERE Id = @Id";
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    var p1 = command.CreateParameter(); p1.ParameterName = "@Id"; p1.Value = id; command.Parameters.Add(p1);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            manutencao = new Manutencao
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                ComputadorMAC = reader["ComputadorMAC"] != DBNull.Value ? reader["ComputadorMAC"].ToString() : null,
                                MonitorPartNumber = reader["MonitorPartNumber"] != DBNull.Value ? reader["MonitorPartNumber"].ToString() : null,
                                PerifericoPartNumber = reader["PerifericoPartNumber"] != DBNull.Value ? reader["PerifericoPartNumber"].ToString() : null,
                                DataManutencaoHardware = reader["DataManutencaoHardware"] != DBNull.Value ? Convert.ToDateTime(reader["DataManutencaoHardware"]) : (DateTime?)null,
                                DataManutencaoSoftware = reader["DataManutencaoSoftware"] != DBNull.Value ? Convert.ToDateTime(reader["DataManutencaoSoftware"]) : (DateTime?)null,
                                ManutencaoExterna = reader["ManutencaoExterna"] != DBNull.Value ? reader["ManutencaoExterna"].ToString() : null,
                                Data = reader["Data"] != DBNull.Value ? Convert.ToDateTime(reader["Data"]) : (DateTime?)null,
                                Historico = reader["Historico"] != DBNull.Value ? reader["Historico"].ToString() : null
                            };
                        }
                    }
                }
            }
            return manutencao;
        }

        public void UpdateManutencao(Manutencao manutencao)
        {
            using (var connection = _databaseService.CreateConnection())
            {
                connection.Open();
                string sql = "UPDATE Manutencoes SET ComputadorMAC = @ComputadorMAC, MonitorPartNumber = @MonitorPartNumber, PerifericoPartNumber = @PerifericoPartNumber, DataManutencaoHardware = @DataManutencaoHardware, DataManutencaoSoftware = @DataManutencaoSoftware, ManutencaoExterna = @ManutencaoExterna, Data = @Data, Historico = @Historico WHERE Id = @Id";
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    var p1 = command.CreateParameter(); p1.ParameterName = "@Id"; p1.Value = manutencao.Id; command.Parameters.Add(p1);
                    var p2 = command.CreateParameter(); p2.ParameterName = "@ComputadorMAC"; p2.Value = (object)manutencao.ComputadorMAC ?? DBNull.Value; command.Parameters.Add(p2);
                    var p3 = command.CreateParameter(); p3.ParameterName = "@MonitorPartNumber"; p3.Value = (object)manutencao.MonitorPartNumber ?? DBNull.Value; command.Parameters.Add(p3);
                    var p4 = command.CreateParameter(); p4.ParameterName = "@PerifericoPartNumber"; p4.Value = (object)manutencao.PerifericoPartNumber ?? DBNull.Value; command.Parameters.Add(p4);
                    var p5 = command.CreateParameter(); p5.ParameterName = "@DataManutencaoHardware"; p5.Value = (object)manutencao.DataManutencaoHardware ?? DBNull.Value; command.Parameters.Add(p5);
                    var p6 = command.CreateParameter(); p6.ParameterName = "@DataManutencaoSoftware"; p6.Value = (object)manutencao.DataManutencaoSoftware ?? DBNull.Value; command.Parameters.Add(p6);
                    var p7 = command.CreateParameter(); p7.ParameterName = "@ManutencaoExterna"; p7.Value = (object)manutencao.ManutencaoExterna ?? DBNull.Value; command.Parameters.Add(p7);
                    var p8 = command.CreateParameter(); p8.ParameterName = "@Data"; p8.Value = (object)manutencao.Data ?? DBNull.Value; command.Parameters.Add(p8);
                    var p9 = command.CreateParameter(); p9.ParameterName = "@Historico"; p9.Value = (object)manutencao.Historico ?? DBNull.Value; command.Parameters.Add(p9);

                    command.ExecuteNonQuery();
                }
            }
        }

        public void DeleteManutencao(int id)
        {
            using (var connection = _databaseService.CreateConnection())
            {
                connection.Open();
                string sql = "DELETE FROM Manutencoes WHERE Id = @Id";
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    var p1 = command.CreateParameter(); p1.ParameterName = "@Id"; p1.Value = id; command.Parameters.Add(p1);
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
