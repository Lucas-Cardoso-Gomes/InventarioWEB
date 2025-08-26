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
                string sql = "SELECT Id, ComputadorMAC, DataManutencaoHardware, DataManutencaoSoftware, ManutencaoExterna, Data, Historico FROM Manutencoes";
                using (var command = new SqlCommand(sql, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            manutencoes.Add(new Manutencao
                            {
                                Id = reader.GetInt32(0),
                                ComputadorMAC = reader.IsDBNull(1) ? null : reader.GetString(1),
                                DataManutencaoHardware = reader.IsDBNull(2) ? (DateTime?)null : reader.GetDateTime(2),
                                DataManutencaoSoftware = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3),
                                ManutencaoExterna = reader.IsDBNull(4) ? null : reader.GetString(4),
                                Data = reader.IsDBNull(5) ? (DateTime?)null : reader.GetDateTime(5),
                                Historico = reader.IsDBNull(6) ? null : reader.GetString(6)
                            });
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
                string sql = "INSERT INTO Manutencoes (ComputadorMAC, DataManutencaoHardware, DataManutencaoSoftware, ManutencaoExterna, Data, Historico) VALUES (@ComputadorMAC, @DataManutencaoHardware, @DataManutencaoSoftware, @ManutencaoExterna, @Data, @Historico)";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@ComputadorMAC", (object)manutencao.ComputadorMAC ?? DBNull.Value);
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
                string sql = "SELECT Id, ComputadorMAC, DataManutencaoHardware, DataManutencaoSoftware, ManutencaoExterna, Data, Historico FROM Manutencoes WHERE Id = @Id";
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
                                DataManutencaoHardware = reader.IsDBNull(2) ? (DateTime?)null : reader.GetDateTime(2),
                                DataManutencaoSoftware = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3),
                                ManutencaoExterna = reader.IsDBNull(4) ? null : reader.GetString(4),
                                Data = reader.IsDBNull(5) ? (DateTime?)null : reader.GetDateTime(5),
                                Historico = reader.IsDBNull(6) ? null : reader.GetString(6)
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
                string sql = "UPDATE Manutencoes SET ComputadorMAC = @ComputadorMAC, DataManutencaoHardware = @DataManutencaoHardware, DataManutencaoSoftware = @DataManutencaoSoftware, ManutencaoExterna = @ManutencaoExterna, Data = @Data, Historico = @Historico WHERE Id = @Id";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Id", manutencao.Id);
                    command.Parameters.AddWithValue("@ComputadorMAC", (object)manutencao.ComputadorMAC ?? DBNull.Value);
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
