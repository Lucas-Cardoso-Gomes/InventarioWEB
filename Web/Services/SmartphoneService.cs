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

                foreach (var sp in smartphones)
                {
                    CarregarIMEIs(connection, sp);
                }
            }
            return await Task.FromResult(smartphones);
        }

        public async Task<Smartphone> GetByIdAsync(int id)
        {
            using (var connection = _databaseService.CreateConnection())
            {
                connection.Open();
                Smartphone sp = null;
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM Smartphones WHERE Id = @Id";
                    var p = command.CreateParameter(); p.ParameterName = "@Id"; p.Value = id; command.Parameters.Add(p);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            sp = MapToSmartphone(reader);
                        }
                    }
                }

                if (sp != null)
                {
                    CarregarIMEIs(connection, sp);
                }
                return sp;
            }
        }

        private void CarregarIMEIs(IDbConnection connection, Smartphone smartphone)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT IMEI, Ordem FROM SmartphoneIMEIs WHERE SmartphoneId = @SmartphoneId ORDER BY Ordem";
                var p = cmd.CreateParameter(); p.ParameterName = "@SmartphoneId"; p.Value = smartphone.Id; cmd.Parameters.Add(p);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int ordem = Convert.ToInt32(reader["Ordem"]);
                        string imei = reader["IMEI"].ToString();
                        if (ordem == 1) smartphone.IMEI1 = imei;
                        else if (ordem == 2) smartphone.IMEI2 = imei;
                    }
                }
            }
        }

        public async Task CreateAsync(Smartphone smartphone)
        {
            using (var connection = _databaseService.CreateConnection())
            {
                connection.Open();
                int insertedId = 0;
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "INSERT INTO Smartphones (Modelo, Usuario, Filial, DataCriacao, ContaGoogle, SenhaGoogle, MAC, DataGarantia) VALUES (@Modelo, @Usuario, @Filial, @DataCriacao, @ContaGoogle, @SenhaGoogle, @MAC, @DataGarantia); SELECT last_insert_rowid();";

                    var p1 = command.CreateParameter(); p1.ParameterName = "@Modelo"; p1.Value = smartphone.Modelo; command.Parameters.Add(p1);
                    var p4 = command.CreateParameter(); p4.ParameterName = "@Usuario"; p4.Value = (object)smartphone.Usuario ?? DBNull.Value; command.Parameters.Add(p4);
                    var p5 = command.CreateParameter(); p5.ParameterName = "@Filial"; p5.Value = (object)smartphone.Filial ?? DBNull.Value; command.Parameters.Add(p5);
                    var p6 = command.CreateParameter(); p6.ParameterName = "@DataCriacao"; p6.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); command.Parameters.Add(p6);
                    var p7 = command.CreateParameter(); p7.ParameterName = "@ContaGoogle"; p7.Value = (object)smartphone.ContaGoogle ?? DBNull.Value; command.Parameters.Add(p7);
                    var p8 = command.CreateParameter(); p8.ParameterName = "@SenhaGoogle"; p8.Value = (object)smartphone.SenhaGoogle ?? DBNull.Value; command.Parameters.Add(p8);
                    var p9 = command.CreateParameter(); p9.ParameterName = "@MAC"; p9.Value = (object)smartphone.MAC ?? DBNull.Value; command.Parameters.Add(p9);
                    var p10 = command.CreateParameter(); p10.ParameterName = "@DataGarantia"; p10.Value = smartphone.DataGarantia.HasValue ? smartphone.DataGarantia.Value.ToString("yyyy-MM-dd HH:mm:ss") : DBNull.Value; command.Parameters.Add(p10);

                    insertedId = Convert.ToInt32(command.ExecuteScalar());
                }

                if (insertedId > 0)
                {
                    smartphone.Id = insertedId;
                    SalvarIMEIs(connection, insertedId, smartphone.IMEI1, smartphone.IMEI2);
                }
            }
            await Task.CompletedTask;
        }

        private void SalvarIMEIs(IDbConnection connection, int smartphoneId, string imei1, string imei2)
        {
            using (var delCmd = connection.CreateCommand())
            {
                delCmd.CommandText = "DELETE FROM SmartphoneIMEIs WHERE SmartphoneId = @SmartphoneId";
                var p = delCmd.CreateParameter(); p.ParameterName = "@SmartphoneId"; p.Value = smartphoneId; delCmd.Parameters.Add(p);
                delCmd.ExecuteNonQuery();
            }

            if (!string.IsNullOrWhiteSpace(imei1))
            {
                using (var insCmd = connection.CreateCommand())
                {
                    insCmd.CommandText = "INSERT INTO SmartphoneIMEIs (SmartphoneId, IMEI, Ordem) VALUES (@SmartphoneId, @IMEI, 1)";
                    var p1 = insCmd.CreateParameter(); p1.ParameterName = "@SmartphoneId"; p1.Value = smartphoneId; insCmd.Parameters.Add(p1);
                    var p2 = insCmd.CreateParameter(); p2.ParameterName = "@IMEI"; p2.Value = imei1; insCmd.Parameters.Add(p2);
                    insCmd.ExecuteNonQuery();
                }
            }

            if (!string.IsNullOrWhiteSpace(imei2))
            {
                using (var insCmd = connection.CreateCommand())
                {
                    insCmd.CommandText = "INSERT INTO SmartphoneIMEIs (SmartphoneId, IMEI, Ordem) VALUES (@SmartphoneId, @IMEI, 2)";
                    var p1 = insCmd.CreateParameter(); p1.ParameterName = "@SmartphoneId"; p1.Value = smartphoneId; insCmd.Parameters.Add(p1);
                    var p2 = insCmd.CreateParameter(); p2.ParameterName = "@IMEI"; p2.Value = imei2; insCmd.Parameters.Add(p2);
                    insCmd.ExecuteNonQuery();
                }
            }
        }

        public async Task UpdateAsync(Smartphone smartphone)
        {
            using (var connection = _databaseService.CreateConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "UPDATE Smartphones SET Modelo = @Modelo, Usuario = @Usuario, Filial = @Filial, DataAlteracao = @DataAlteracao, ContaGoogle = @ContaGoogle, SenhaGoogle = @SenhaGoogle, MAC = @MAC, DataGarantia = @DataGarantia WHERE Id = @Id";

                    var p1 = command.CreateParameter(); p1.ParameterName = "@Id"; p1.Value = smartphone.Id; command.Parameters.Add(p1);
                    var p2 = command.CreateParameter(); p2.ParameterName = "@Modelo"; p2.Value = smartphone.Modelo; command.Parameters.Add(p2);
                    var p5 = command.CreateParameter(); p5.ParameterName = "@Usuario"; p5.Value = (object)smartphone.Usuario ?? DBNull.Value; command.Parameters.Add(p5);
                    var p6 = command.CreateParameter(); p6.ParameterName = "@Filial"; p6.Value = (object)smartphone.Filial ?? DBNull.Value; command.Parameters.Add(p6);
                    var p7 = command.CreateParameter(); p7.ParameterName = "@DataAlteracao"; p7.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); command.Parameters.Add(p7);
                    var p8 = command.CreateParameter(); p8.ParameterName = "@ContaGoogle"; p8.Value = (object)smartphone.ContaGoogle ?? DBNull.Value; command.Parameters.Add(p8);
                    var p9 = command.CreateParameter(); p9.ParameterName = "@SenhaGoogle"; p9.Value = (object)smartphone.SenhaGoogle ?? DBNull.Value; command.Parameters.Add(p9);
                    var p10 = command.CreateParameter(); p10.ParameterName = "@MAC"; p10.Value = (object)smartphone.MAC ?? DBNull.Value; command.Parameters.Add(p10);
                    var p11 = command.CreateParameter(); p11.ParameterName = "@DataGarantia"; p11.Value = smartphone.DataGarantia.HasValue ? smartphone.DataGarantia.Value.ToString("yyyy-MM-dd HH:mm:ss") : DBNull.Value; command.Parameters.Add(p11);

                    command.ExecuteNonQuery();
                }

                SalvarIMEIs(connection, smartphone.Id, smartphone.IMEI1, smartphone.IMEI2);
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
                Usuario = reader["Usuario"] != DBNull.Value ? reader["Usuario"].ToString() : null,
                Filial = reader["Filial"] != DBNull.Value ? reader["Filial"].ToString() : null,
                DataCriacao = Convert.ToDateTime(reader["DataCriacao"]),
                DataAlteracao = reader["DataAlteracao"] != DBNull.Value ? Convert.ToDateTime(reader["DataAlteracao"]) : (DateTime?)null,
                ContaGoogle = reader["ContaGoogle"] != DBNull.Value ? reader["ContaGoogle"].ToString() : null,
                SenhaGoogle = reader["SenhaGoogle"] != DBNull.Value ? reader["SenhaGoogle"].ToString() : null,
                MAC = reader["MAC"] != DBNull.Value ? reader["MAC"].ToString() : null,
                DataGarantia = reader["DataGarantia"] != DBNull.Value ? Convert.ToDateTime(reader["DataGarantia"]) : (DateTime?)null
            };
        }
    }
}
