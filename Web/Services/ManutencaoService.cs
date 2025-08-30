using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Web.Models;

namespace Web.Services
{
    public class ManutencaoService
    {
        private readonly string _connectionString;

        public ManutencaoService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public List<Manutencao> GetAllManutencoes()
        {
            var manutencoes = new List<Manutencao>();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string sql = @"
                    SELECT
                        m.Id, m.DataManutencaoHardware, m.DataManutencaoSoftware, m.ManutencaoExterna, m.Data, m.Historico,
                        c.MAC, c.Hostname,
                        mo.PartNumber as MonitorPN, mo.Modelo,
                        p.PartNumber as PerifericoPN, p.Tipo
                    FROM Manutencoes m
                    LEFT JOIN Computadores c ON m.ComputadorMAC = c.MAC
                    LEFT JOIN Monitores mo ON m.MonitorPartNumber = mo.PartNumber
                    LEFT JOIN Perifericos p ON m.PerifericoPartNumber = p.PartNumber";

                using (var command = new SqlCommand(sql, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var manutencao = new Manutencao
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                DataManutencaoHardware = reader.IsDBNull(reader.GetOrdinal("DataManutencaoHardware")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("DataManutencaoHardware")),
                                DataManutencaoSoftware = reader.IsDBNull(reader.GetOrdinal("DataManutencaoSoftware")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("DataManutencaoSoftware")),
                                ManutencaoExterna = reader.IsDBNull(reader.GetOrdinal("ManutencaoExterna")) ? null : reader.GetString(reader.GetOrdinal("ManutencaoExterna")),
                                Data = reader.IsDBNull(reader.GetOrdinal("Data")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("Data")),
                                Historico = reader.IsDBNull(reader.GetOrdinal("Historico")) ? null : reader.GetString(reader.GetOrdinal("Historico")),
                                ComputadorMAC = reader.IsDBNull(reader.GetOrdinal("MAC")) ? null : reader.GetString(reader.GetOrdinal("MAC")),
                                MonitorPartNumber = reader.IsDBNull(reader.GetOrdinal("MonitorPN")) ? null : reader.GetString(reader.GetOrdinal("MonitorPN")),
                                PerifericoPartNumber = reader.IsDBNull(reader.GetOrdinal("PerifericoPN")) ? null : reader.GetString(reader.GetOrdinal("PerifericoPN"))
                            };

                            if (manutencao.ComputadorMAC != null)
                            {
                                manutencao.Computador = new Computador { MAC = manutencao.ComputadorMAC, Hostname = reader.GetString(reader.GetOrdinal("Hostname")) };
                            }
                            if (manutencao.MonitorPartNumber != null)
                            {
                                manutencao.Monitor = new Monitor { PartNumber = manutencao.MonitorPartNumber, Modelo = reader.GetString(reader.GetOrdinal("Modelo")) };
                            }
                            if (manutencao.PerifericoPartNumber != null)
                            {
                                manutencao.Periferico = new Periferico { PartNumber = manutencao.PerifericoPartNumber, Tipo = reader.GetString(reader.GetOrdinal("Tipo")) };
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
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string sql = "INSERT INTO Manutencoes (ComputadorMAC, MonitorPartNumber, PerifericoPartNumber, DataManutencaoHardware, DataManutencaoSoftware, ManutencaoExterna, Data, Historico) VALUES (@ComputadorMAC, @MonitorPartNumber, @PerifericoPartNumber, @DataManutencaoHardware, @DataManutencaoSoftware, @ManutencaoExterna, @Data, @Historico)";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@ComputadorMAC", (object)manutencao.ComputadorMAC ?? DBNull.Value);
                    command.Parameters.AddWithValue("@MonitorPartNumber", (object)manutencao.MonitorPartNumber ?? DBNull.Value);
                    command.Parameters.AddWithValue("@PerifericoPartNumber", (object)manutencao.PerifericoPartNumber ?? DBNull.Value);
                    command.Parameters.AddWithValue("@DataManutencaoHardware", (object)manutencao.DataManutencaoHardware ?? DBNull.Value);
                    command.Parameters.AddWithValue("@DataManutencaoSoftware", (object)manutencao.DataManutencaoSoftware ?? DBNull.Value);
                    command.Parameters.AddWithValue("@ManutencaoExterna", (object)manutencao.ManutencaoExterna ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Data", (object)manutencao.Data ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Historico", (object)manutencao.Historico ?? DBNull.Value);
                    command.ExecuteNonQuery();
                }
            }
        }

        public Manutencao GetManutencaoById(int id)
        {
            Manutencao manutencao = null;
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string sql = "SELECT Id, ComputadorMAC, MonitorPartNumber, PerifericoPartNumber, DataManutencaoHardware, DataManutencaoSoftware, ManutencaoExterna, Data, Historico FROM Manutencoes WHERE Id = @Id";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            manutencao = new Manutencao
                            {
                                Id = reader.GetInt32(0),
                                ComputadorMAC = reader.IsDBNull(1) ? null : reader.GetString(1),
                                MonitorPartNumber = reader.IsDBNull(2) ? null : reader.GetString(2),
                                PerifericoPartNumber = reader.IsDBNull(3) ? null : reader.GetString(3),
                                DataManutencaoHardware = reader.IsDBNull(4) ? (DateTime?)null : reader.GetDateTime(4),
                                DataManutencaoSoftware = reader.IsDBNull(5) ? (DateTime?)null : reader.GetDateTime(5),
                                ManutencaoExterna = reader.IsDBNull(6) ? null : reader.GetString(6),
                                Data = reader.IsDBNull(7) ? (DateTime?)null : reader.GetDateTime(7),
                                Historico = reader.IsDBNull(8) ? null : reader.GetString(8)
                            };
                        }
                    }
                }
            }
            return manutencao;
        }

        public void UpdateManutencao(Manutencao manutencao)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string sql = "UPDATE Manutencoes SET ComputadorMAC = @ComputadorMAC, MonitorPartNumber = @MonitorPartNumber, PerifericoPartNumber = @PerifericoPartNumber, DataManutencaoHardware = @DataManutencaoHardware, DataManutencaoSoftware = @DataManutencaoSoftware, ManutencaoExterna = @ManutencaoExterna, Data = @Data, Historico = @Historico WHERE Id = @Id";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Id", manutencao.Id);
                    command.Parameters.AddWithValue("@ComputadorMAC", (object)manutencao.ComputadorMAC ?? DBNull.Value);
                    command.Parameters.AddWithValue("@MonitorPartNumber", (object)manutencao.MonitorPartNumber ?? DBNull.Value);
                    command.Parameters.AddWithValue("@PerifericoPartNumber", (object)manutencao.PerifericoPartNumber ?? DBNull.Value);
                    command.Parameters.AddWithValue("@DataManutencaoHardware", (object)manutencao.DataManutencaoHardware ?? DBNull.Value);
                    command.Parameters.AddWithValue("@DataManutencaoSoftware", (object)manutencao.DataManutencaoSoftware ?? DBNull.Value);
                    command.Parameters.AddWithValue("@ManutencaoExterna", (object)manutencao.ManutencaoExterna ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Data", (object)manutencao.Data ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Historico", (object)manutencao.Historico ?? DBNull.Value);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void DeleteManutencao(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string sql = "DELETE FROM Manutencoes WHERE Id = @Id";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
