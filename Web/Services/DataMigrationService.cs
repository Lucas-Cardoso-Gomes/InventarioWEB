using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Data.Sqlite;
using System.Threading.Tasks;

namespace Web.Services
{
    public class DataMigrationService
    {
        private readonly IDatabaseService _destinationDb;
        private readonly ILogger<DataMigrationService> _logger;

        public DataMigrationService(IDatabaseService destinationDb, ILogger<DataMigrationService> logger)
        {
            _destinationDb = destinationDb;
            _logger = logger;
        }

        public async Task MigrateAsync(string sourceConnectionString)
        {
            _logger.LogInformation("Starting migration from SQL Server...");

            try
            {
                using (var sourceConnection = new SqlConnection(sourceConnectionString))
                {
                    await sourceConnection.OpenAsync();

                    using (var destConnection = _destinationDb.CreateConnection())
                    {
                        destConnection.Open();

                        // Disable FKs temporarily
                        using (var cmd = destConnection.CreateCommand())
                        {
                            cmd.CommandText = "PRAGMA foreign_keys = OFF;";
                            cmd.ExecuteNonQuery();
                        }

                        using (var transaction = destConnection.BeginTransaction())
                        {
                            try
                            {
                                await MigrateTableAsync(sourceConnection, destConnection, transaction, "Colaboradores",
                                    "CPF, Nome, Email, SenhaEmail, Teams, SenhaTeams, EDespacho, SenhaEDespacho, Genius, SenhaGenius, Ibrooker, SenhaIbrooker, Adicional, SenhaAdicional, Filial, Setor, Smartphone, TelefoneFixo, Ramal, Alarme, Videoporteiro, Obs, DataInclusao, DataAlteracao, CoordenadorCPF");

                                await MigrateTableAsync(sourceConnection, destConnection, transaction, "Usuarios",
                                    "Id, Nome, Login, PasswordHash, Role, ColaboradorCPF, IsCoordinator", identityInsert: true);

                                await MigrateTableAsync(sourceConnection, destConnection, transaction, "Rede",
                                    "Id, Tipo, IP, MAC, Nome, DataInclusao, DataAlteracao, Observacao", identityInsert: true);

                                await MigrateTableAsync(sourceConnection, destConnection, transaction, "Smartphones",
                                    "Id, Modelo, IMEI1, IMEI2, Usuario, Filial, DataCriacao, DataAlteracao, ContaGoogle, SenhaGoogle, MAC", identityInsert: true);

                                await MigrateTableAsync(sourceConnection, destConnection, transaction, "Computadores",
                                    "MAC, IP, ColaboradorCPF, Hostname, Fabricante, Processador, ProcessadorFabricante, ProcessadorCore, ProcessadorThread, ProcessadorClock, Ram, RamTipo, RamVelocidade, RamVoltagem, RamPorModule, ArmazenamentoC, ArmazenamentoCTotal, ArmazenamentoCLivre, ArmazenamentoD, ArmazenamentoDTotal, ArmazenamentoDLivre, ConsumoCPU, SO, DataColeta, PartNumber");

                                await MigrateTableAsync(sourceConnection, destConnection, transaction, "Monitores",
                                    "PartNumber, ColaboradorCPF, Marca, Modelo, Tamanho");

                                await MigrateTableAsync(sourceConnection, destConnection, transaction, "Perifericos",
                                    "PartNumber, ColaboradorCPF, Tipo, DataEntrega");

                                await MigrateTableAsync(sourceConnection, destConnection, transaction, "Chamados",
                                    "ID, AdminCPF, ColaboradorCPF, Servico, Descricao, DataAlteracao, DataCriacao, Status, Prioridade", identityInsert: true);

                                await MigrateTableAsync(sourceConnection, destConnection, transaction, "ChamadoConversas",
                                    "ID, ChamadoID, UsuarioCPF, Mensagem, DataCriacao", identityInsert: true);

                                await MigrateTableAsync(sourceConnection, destConnection, transaction, "ChamadoAnexos",
                                    "ID, ChamadoID, NomeArquivo, CaminhoArquivo, DataUpload", identityInsert: true);

                                await MigrateTableAsync(sourceConnection, destConnection, transaction, "Manutencoes",
                                    "Id, ComputadorMAC, MonitorPartNumber, PerifericoPartNumber, DataManutencaoHardware, DataManutencaoSoftware, ManutencaoExterna, Data, Historico", identityInsert: true);

                                await MigrateTableAsync(sourceConnection, destConnection, transaction, "PersistentLogs",
                                    "Id, Timestamp, EntityType, ActionType, PerformedBy, Details", identityInsert: true);

                                await MigrateTableAsync(sourceConnection, destConnection, transaction, "Logs",
                                    "Id, Timestamp, Level, Message, Source", identityInsert: true);

                                transaction.Commit();
                                _logger.LogInformation("Migration completed successfully.");
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                _logger.LogError(ex, "Error during migration transaction. Rolling back.");
                                throw;
                            }
                        }

                        // Re-enable FKs
                        using (var cmd = destConnection.CreateCommand())
                        {
                            cmd.CommandText = "PRAGMA foreign_keys = ON;";
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to source database or execute migration.");
                throw;
            }
        }

        private async Task MigrateTableAsync(SqlConnection source, IDbConnection dest, IDbTransaction transaction, string tableName, string columns, bool identityInsert = false)
        {
            _logger.LogInformation($"Migrating table {tableName}...");

            // Clear destination table first
            using (var clearCmd = dest.CreateCommand())
            {
                clearCmd.Transaction = transaction;
                clearCmd.CommandText = $"DELETE FROM {tableName}";
                clearCmd.ExecuteNonQuery();
            }

            string selectSql = $"SELECT {columns} FROM {tableName}";
            using (var sourceCmd = new SqlCommand(selectSql, source))
            using (var reader = await sourceCmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var colNames = columns.Split(',');
                    var paramNames = new List<string>();

                    using (var destCmd = dest.CreateCommand())
                    {
                        destCmd.Transaction = transaction;

                        for (int i = 0; i < colNames.Length; i++)
                        {
                            string colName = colNames[i].Trim();
                            string paramName = $"@p{i}";
                            paramNames.Add(paramName);

                            var p = destCmd.CreateParameter();
                            p.ParameterName = paramName;

                            var val = reader[colName]; // Access by column name is safer

                            // Convert DateTime to string for SQLite
                            if (val is DateTime dt)
                            {
                                p.Value = dt.ToString("yyyy-MM-dd HH:mm:ss");
                            }
                            else
                            {
                                p.Value = val;
                            }

                            destCmd.Parameters.Add(p);
                        }

                        destCmd.CommandText = $"INSERT INTO {tableName} ({columns}) VALUES ({string.Join(", ", paramNames)})";
                        destCmd.ExecuteNonQuery();
                    }
                }
            }
            _logger.LogInformation($"Table {tableName} migrated.");
        }
    }
}
