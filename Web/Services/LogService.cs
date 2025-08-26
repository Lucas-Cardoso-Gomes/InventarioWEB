using System;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using web.Models;

namespace Web.Services
{
    public class LogService
    {
        private readonly string _connectionString;
        private readonly ILogger<LogService> _logger;

        public LogService(IConfiguration configuration, ILogger<LogService> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
        }

        public void AddLog(string level, string message, string source)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    string sql = "INSERT INTO Logs (Timestamp, Level, Message, Source) VALUES (@Timestamp, @Level, @Message, @Source)";

                    using (SqlCommand cmd = new SqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@Timestamp", DateTime.Now);
                        cmd.Parameters.AddWithValue("@Level", level);
                        cmd.Parameters.AddWithValue("@Message", message);
                        cmd.Parameters.AddWithValue("@Source", (object)source ?? DBNull.Value);

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
