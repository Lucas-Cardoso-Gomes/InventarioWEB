using System;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Web.Models;
using System.Data;

namespace Web.Services
{
    public class LogService
    {
        private readonly IDatabaseService _databaseService;
        private readonly ILogger<LogService> _logger;

        public LogService(IDatabaseService databaseService, ILogger<LogService> logger)
        {
            _databaseService = databaseService;
            _logger = logger;
        }

        public void AddLog(string level, string message, string source)
        {
            try
            {
                using (var connection = _databaseService.CreateLogsConnection())
                {
                    connection.Open();

                    string sql = "INSERT INTO Logs (Timestamp, Level, Message, Source) VALUES (@Timestamp, @Level, @Message, @Source)";

                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = sql;
                        var p1 = cmd.CreateParameter(); p1.ParameterName = "@Timestamp"; p1.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); cmd.Parameters.Add(p1);
                        var p2 = cmd.CreateParameter(); p2.ParameterName = "@Level"; p2.Value = level; cmd.Parameters.Add(p2);
                        var p3 = cmd.CreateParameter(); p3.ParameterName = "@Message"; p3.Value = message; cmd.Parameters.Add(p3);
                        var p4 = cmd.CreateParameter(); p4.ParameterName = "@Source"; p4.Value = (object)source ?? DBNull.Value; cmd.Parameters.Add(p4);

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                // Fallback logging to the console if database logging fails.
                _logger.LogError(ex, "Falha ao gravar no banco de dados de log. Log original: Level={Level}, Source={Source}, Message={Message}", level, source, message);
            }
        }
    }
}
