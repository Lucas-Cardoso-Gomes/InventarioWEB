using Microsoft.Extensions.Configuration;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Threading.Tasks;
using Web.Models;
using System;
using System.Data;

namespace Web.Services
{
    public class SmartphoneService
    {
        private readonly IDatabaseService _databaseService;

        public SmartphoneService(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public async Task<IEnumerable<Smartphone>> GetAllAsync()
        {
            var smartphones = new List<Smartphone>();
            using (var connection = _databaseService.CreateConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM Smartphones";
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            smartphones.Add(MapToSmartphone(reader));
                        }
                    }
                }
            }
            return await Task.FromResult(smartphones);
        }

        public async Task<Smartphone> GetByIdAsync(int id)
        {
            using (var connection = _databaseService.CreateConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM Smartphones WHERE Id = @Id";
                    var p = command.CreateParameter(); p.ParameterName = "@Id"; p.Value = id; command.Parameters.Add(p);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return MapToSmartphone(reader);
                        }
                    }
                }
            }
            return null;
        }

        public async Task CreateAsync(Smartphone smartphone)
        {
            using (var connection = _databaseService.CreateConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "INSERT INTO Smartphones (Modelo, IMEI1, IMEI2, Usuario, Filial, DataCriacao, ContaGoogle, SenhaGoogle, MAC) VALUES (@Modelo, @IMEI1, @IMEI2, @Usuario, @Filial, @DataCriacao, @ContaGoogle, @SenhaGoogle, @MAC)";

                    var p1 = command.CreateParameter(); p1.ParameterName = "@Modelo"; p1.Value = smartphone.Modelo; command.Parameters.Add(p1);
                    var p2 = command.CreateParameter(); p2.ParameterName = "@IMEI1"; p2.Value = smartphone.IMEI1; command.Parameters.Add(p2);
                    var p3 = command.CreateParameter(); p3.ParameterName = "@IMEI2"; p3.Value = (object)smartphone.IMEI2 ?? DBNull.Value; command.Parameters.Add(p3);
                    var p4 = command.CreateParameter(); p4.ParameterName = "@Usuario"; p4.Value = (object)smartphone.Usuario ?? DBNull.Value; command.Parameters.Add(p4);
                    var p5 = command.CreateParameter(); p5.ParameterName = "@Filial"; p5.Value = (object)smartphone.Filial ?? DBNull.Value; command.Parameters.Add(p5);
                    var p6 = command.CreateParameter(); p6.ParameterName = "@DataCriacao"; p6.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); command.Parameters.Add(p6);
                    var p7 = command.CreateParameter(); p7.ParameterName = "@ContaGoogle"; p7.Value = (object)smartphone.ContaGoogle ?? DBNull.Value; command.Parameters.Add(p7);
                    var p8 = command.CreateParameter(); p8.ParameterName = "@SenhaGoogle"; p8.Value = (object)smartphone.SenhaGoogle ?? DBNull.Value; command.Parameters.Add(p8);
                    var p9 = command.CreateParameter(); p9.ParameterName = "@MAC"; p9.Value = (object)smartphone.MAC ?? DBNull.Value; command.Parameters.Add(p9);

                    command.ExecuteNonQuery();
                }
            }
            await Task.CompletedTask;
        }

        public async Task UpdateAsync(Smartphone smartphone)
        {
            using (var connection = _databaseService.CreateConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "UPDATE Smartphones SET Modelo = @Modelo, IMEI1 = @IMEI1, IMEI2 = @IMEI2, Usuario = @Usuario, Filial = @Filial, DataAlteracao = @DataAlteracao, ContaGoogle = @ContaGoogle, SenhaGoogle = @SenhaGoogle, MAC = @MAC WHERE Id = @Id";

                    var p1 = command.CreateParameter(); p1.ParameterName = "@Id"; p1.Value = smartphone.Id; command.Parameters.Add(p1);
                    var p2 = command.CreateParameter(); p2.ParameterName = "@Modelo"; p2.Value = smartphone.Modelo; command.Parameters.Add(p2);
                    var p3 = command.CreateParameter(); p3.ParameterName = "@IMEI1"; p3.Value = smartphone.IMEI1; command.Parameters.Add(p3);
                    var p4 = command.CreateParameter(); p4.ParameterName = "@IMEI2"; p4.Value = (object)smartphone.IMEI2 ?? DBNull.Value; command.Parameters.Add(p4);
                    var p5 = command.CreateParameter(); p5.ParameterName = "@Usuario"; p5.Value = (object)smartphone.Usuario ?? DBNull.Value; command.Parameters.Add(p5);
                    var p6 = command.CreateParameter(); p6.ParameterName = "@Filial"; p6.Value = (object)smartphone.Filial ?? DBNull.Value; command.Parameters.Add(p6);
                    var p7 = command.CreateParameter(); p7.ParameterName = "@DataAlteracao"; p7.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); command.Parameters.Add(p7);
                    var p8 = command.CreateParameter(); p8.ParameterName = "@ContaGoogle"; p8.Value = (object)smartphone.ContaGoogle ?? DBNull.Value; command.Parameters.Add(p8);
                    var p9 = command.CreateParameter(); p9.ParameterName = "@SenhaGoogle"; p9.Value = (object)smartphone.SenhaGoogle ?? DBNull.Value; command.Parameters.Add(p9);
                    var p10 = command.CreateParameter(); p10.ParameterName = "@MAC"; p10.Value = (object)smartphone.MAC ?? DBNull.Value; command.Parameters.Add(p10);

                    command.ExecuteNonQuery();
                }
            }
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(int id)
        {
            using (var connection = _databaseService.CreateConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "DELETE FROM Smartphones WHERE Id = @Id";
                    var p = command.CreateParameter(); p.ParameterName = "@Id"; p.Value = id; command.Parameters.Add(p);
                    command.ExecuteNonQuery();
                }
            }
            await Task.CompletedTask;
        }

        private Smartphone MapToSmartphone(IDataReader reader)
        {
            return new Smartphone
            {
                Id = Convert.ToInt32(reader["Id"]),
                Modelo = reader["Modelo"].ToString(),
                IMEI1 = reader["IMEI1"].ToString(),
                IMEI2 = reader["IMEI2"] != DBNull.Value ? reader["IMEI2"].ToString() : null,
                Usuario = reader["Usuario"] != DBNull.Value ? reader["Usuario"].ToString() : null,
                Filial = reader["Filial"] != DBNull.Value ? reader["Filial"].ToString() : null,
                DataCriacao = Convert.ToDateTime(reader["DataCriacao"]),
                DataAlteracao = reader["DataAlteracao"] != DBNull.Value ? Convert.ToDateTime(reader["DataAlteracao"]) : (DateTime?)null,
                ContaGoogle = reader["ContaGoogle"] != DBNull.Value ? reader["ContaGoogle"].ToString() : null,
                SenhaGoogle = reader["SenhaGoogle"] != DBNull.Value ? reader["SenhaGoogle"].ToString() : null,
                MAC = reader["MAC"] != DBNull.Value ? reader["MAC"].ToString() : null
            };
        }
    }
}
