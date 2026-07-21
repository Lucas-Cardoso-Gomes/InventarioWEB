using System;
using System.Data;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace Web.Services
{
    using System.Collections.Generic;
    using Web.Models;

    public interface IHistoricoTrocasService
    {
        void RegistrarAlteracao(string equipamentoId, string tipoEquipamento, string campoAlterado, string valorAntigo, string valorNovo, string usuario);
        List<HistoricoTroca> GetHistoricoByEquipamento(string tipoEquipamento, string equipamentoId);
    }

    public class HistoricoTrocasService : IHistoricoTrocasService
    {
        private readonly IDatabaseService _databaseService;
        private readonly ILogger<HistoricoTrocasService> _logger;

        public HistoricoTrocasService(IDatabaseService databaseService, ILogger<HistoricoTrocasService> logger)
        {
            _databaseService = databaseService;
            _logger = logger;
        }

        public void RegistrarAlteracao(string equipamentoId, string tipoEquipamento, string campoAlterado, string valorAntigo, string valorNovo, string usuario)
        {
            if (valorAntigo == valorNovo) return;

            try
            {
                using (var connection = _databaseService.CreateConnection())
                {
                    connection.Open();
                    string sql = @"INSERT INTO HistoricoTrocas (EquipamentoId, TipoEquipamento, CampoAlterado, ValorAntigo, ValorNovo, DataAlteracao, Usuario)
                                   VALUES (@EquipamentoId, @TipoEquipamento, @CampoAlterado, @ValorAntigo, @ValorNovo, @DataAlteracao, @Usuario)";

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = sql;
                        AddParameter(command, "@EquipamentoId", equipamentoId);
                        AddParameter(command, "@TipoEquipamento", tipoEquipamento);
                        AddParameter(command, "@CampoAlterado", campoAlterado);
                        AddParameter(command, "@ValorAntigo", valorAntigo ?? "");
                        AddParameter(command, "@ValorNovo", valorNovo ?? "");
                        AddParameter(command, "@DataAlteracao", DateTime.UtcNow.ToString("O"));
                        AddParameter(command, "@Usuario", usuario);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao registrar histórico de trocas.");
            }
        }

        public List<HistoricoTroca> GetHistoricoByEquipamento(string tipoEquipamento, string equipamentoId)
        {
            var historico = new List<HistoricoTroca>();
            try
            {
                using (var connection = _databaseService.CreateConnection())
                {
                    connection.Open();
                    string sql = @"SELECT * FROM HistoricoTrocas
                                   WHERE TipoEquipamento = @TipoEquipamento AND EquipamentoId = @EquipamentoId
                                   ORDER BY DataAlteracao DESC";

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = sql;
                        AddParameter(command, "@TipoEquipamento", tipoEquipamento);
                        AddParameter(command, "@EquipamentoId", equipamentoId);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                historico.Add(new HistoricoTroca
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    EquipamentoId = reader["EquipamentoId"].ToString(),
                                    TipoEquipamento = reader["TipoEquipamento"].ToString(),
                                    CampoAlterado = reader["CampoAlterado"].ToString(),
                                    ValorAntigo = reader["ValorAntigo"]?.ToString(),
                                    ValorNovo = reader["ValorNovo"]?.ToString(),
                                    DataAlteracao = reader["DataAlteracao"].ToString(),
                                    Usuario = reader["Usuario"].ToString()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar histórico de trocas.");
            }
            return historico;
        }

        private void AddParameter(IDbCommand command, string name, object value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }
    }
}
