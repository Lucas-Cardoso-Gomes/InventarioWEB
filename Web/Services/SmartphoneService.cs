using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Threading.Tasks;
using Web.Models;
using System;

namespace Web.Services
{
    public class SmartphoneService
    {
        private readonly string _connectionString;

        public SmartphoneService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<IEnumerable<Smartphone>> GetAllAsync()
        {
            var smartphones = new List<Smartphone>();
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand("SELECT * FROM Smartphones", connection);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        smartphones.Add(MapToSmartphone(reader));
                    }
                }
            }
            return smartphones;
        }

        public async Task<Smartphone> GetByIdAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand("SELECT * FROM Smartphones WHERE Id = @Id", connection);
                command.Parameters.AddWithValue("@Id", id);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return MapToSmartphone(reader);
                    }
                }
            }
            return null;
        }

        public async Task CreateAsync(Smartphone smartphone)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand(
                    "INSERT INTO Smartphones (Modelo, IMEI1, IMEI2, Usuario, Filial, DataCriacao, ContaGoogle, SenhaGoogle) VALUES (@Modelo, @IMEI1, @IMEI2, @Usuario, @Filial, @DataCriacao, @ContaGoogle, @SenhaGoogle)",
                    connection);
                
                command.Parameters.AddWithValue("@Modelo", smartphone.Modelo);
                command.Parameters.AddWithValue("@IMEI1", smartphone.IMEI1);
                command.Parameters.AddWithValue("@IMEI2", (object)smartphone.IMEI2 ?? DBNull.Value);
                command.Parameters.AddWithValue("@Usuario", (object)smartphone.Usuario ?? DBNull.Value);
                command.Parameters.AddWithValue("@Filial", (object)smartphone.Filial ?? DBNull.Value);
                command.Parameters.AddWithValue("@DataCriacao", DateTime.Now);
                command.Parameters.AddWithValue("@ContaGoogle", (object)smartphone.ContaGoogle ?? DBNull.Value);
                command.Parameters.AddWithValue("@SenhaGoogle", (object)smartphone.SenhaGoogle ?? DBNull.Value);
                
                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task UpdateAsync(Smartphone smartphone)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand(
                    "UPDATE Smartphones SET Modelo = @Modelo, IMEI1 = @IMEI1, IMEI2 = @IMEI2, Usuario = @Usuario, Filial = @Filial, DataAlteracao = @DataAlteracao, ContaGoogle = @ContaGoogle, SenhaGoogle = @SenhaGoogle WHERE Id = @Id",
                    connection);

                command.Parameters.AddWithValue("@Id", smartphone.Id);
                command.Parameters.AddWithValue("@Modelo", smartphone.Modelo);
                command.Parameters.AddWithValue("@IMEI1", smartphone.IMEI1);
                command.Parameters.AddWithValue("@IMEI2", (object)smartphone.IMEI2 ?? DBNull.Value);
                command.Parameters.AddWithValue("@Usuario", (object)smartphone.Usuario ?? DBNull.Value);
                command.Parameters.AddWithValue("@Filial", (object)smartphone.Filial ?? DBNull.Value);
                command.Parameters.AddWithValue("@DataAlteracao", DateTime.Now);
                command.Parameters.AddWithValue("@ContaGoogle", (object)smartphone.ContaGoogle ?? DBNull.Value);
                command.Parameters.AddWithValue("@SenhaGoogle", (object)smartphone.SenhaGoogle ?? DBNull.Value);

                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task DeleteAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand("DELETE FROM Smartphones WHERE Id = @Id", connection);
                command.Parameters.AddWithValue("@Id", id);
                await command.ExecuteNonQueryAsync();
            }
        }

        private Smartphone MapToSmartphone(SqlDataReader reader)
        {
            return new Smartphone
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Modelo = reader.GetString(reader.GetOrdinal("Modelo")),
                IMEI1 = reader.GetString(reader.GetOrdinal("IMEI1")),
                IMEI2 = reader.IsDBNull(reader.GetOrdinal("IMEI2")) ? null : reader.GetString(reader.GetOrdinal("IMEI2")),
                Usuario = reader.IsDBNull(reader.GetOrdinal("Usuario")) ? null : reader.GetString(reader.GetOrdinal("Usuario")),
                Filial = reader.IsDBNull(reader.GetOrdinal("Filial")) ? null : reader.GetString(reader.GetOrdinal("Filial")),
                DataCriacao = reader.GetDateTime(reader.GetOrdinal("DataCriacao")),
                DataAlteracao = reader.IsDBNull(reader.GetOrdinal("DataAlteracao")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("DataAlteracao")),
                ContaGoogle = reader.IsDBNull(reader.GetOrdinal("ContaGoogle")) ? null : reader.GetString(reader.GetOrdinal("ContaGoogle")),
                SenhaGoogle = reader.IsDBNull(reader.GetOrdinal("SenhaGoogle")) ? null : reader.GetString(reader.GetOrdinal("SenhaGoogle"))
            };
        }
    }
}