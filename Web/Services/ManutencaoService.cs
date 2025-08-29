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
                string sql = "SELECT Id, ComputadorMAC, Data, Descricao, Custo FROM Manutencoes";
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
                                Data = reader.GetDateTime(2),
                                Descricao = reader.GetString(3),
                                Custo = reader.GetDecimal(4)
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
                string sql = "INSERT INTO Manutencoes (ComputadorMAC, Data, Descricao, Custo) VALUES (@ComputadorMAC, @Data, @Descricao, @Custo)";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@ComputadorMAC", manutencao.ComputadorMAC);
                    command.Parameters.AddWithValue("@Data", manutencao.Data);
                    command.Parameters.AddWithValue("@Descricao", manutencao.Descricao);
                    command.Parameters.AddWithValue("@Custo", (object)manutencao.Custo ?? DBNull.Value);
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
                string sql = "SELECT Id, ComputadorMAC, Data, Descricao, Custo FROM Manutencoes WHERE Id = @Id";
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
                                ComputadorMAC = reader.GetString(1),
                                Data = reader.GetDateTime(2),
                                Descricao = reader.GetString(3),
                                Custo = reader.GetDecimal(4)
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
                string sql = "UPDATE Manutencoes SET ComputadorMAC = @ComputadorMAC, Data = @Data, Descricao = @Descricao, Custo = @Custo WHERE Id = @Id";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Id", manutencao.Id);
                    command.Parameters.AddWithValue("@ComputadorMAC", manutencao.ComputadorMAC);
                    command.Parameters.AddWithValue("@Data", manutencao.Data);
                    command.Parameters.AddWithValue("@Descricao", manutencao.Descricao);
                    command.Parameters.AddWithValue("@Custo", (object)manutencao.Custo ?? DBNull.Value);
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
