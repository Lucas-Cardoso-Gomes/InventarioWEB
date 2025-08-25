using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using web.Models;

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
                string sql = "SELECT Id, ComputadorMAC, DataManutencaoHardware, DataManutencaoSoftware, ManutencaoExterna, Data FROM Manutencoes";
                using (var command = new SqlCommand(sql, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            manutencoes.Add(new Manutencao
                            {
                                Id = reader.GetInt32(0),
                                ComputadorMAC = reader.GetString(1),
                                DataManutencaoHardware = reader.IsDBNull(2) ? (DateTime?)null : reader.GetDateTime(2),
                                DataManutencaoSoftware = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3),
                                ManutencaoExterna = reader.IsDBNull(4) ? null : reader.GetString(4),
                                Data = reader.GetDateTime(5)
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
                string sql = "INSERT INTO Manutencoes (ComputadorMAC, DataManutencaoHardware, DataManutencaoSoftware, ManutencaoExterna, Data) VALUES (@ComputadorMAC, @DataManutencaoHardware, @DataManutencaoSoftware, @ManutencaoExterna, @Data)";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@ComputadorMAC", manutencao.ComputadorMAC);
                    command.Parameters.AddWithValue("@DataManutencaoHardware", (object)manutencao.DataManutencaoHardware ?? DBNull.Value);
                    command.Parameters.AddWithValue("@DataManutencaoSoftware", (object)manutencao.DataManutencaoSoftware ?? DBNull.Value);
                    command.Parameters.AddWithValue("@ManutencaoExterna", (object)manutencao.ManutencaoExterna ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Data", manutencao.Data);
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
