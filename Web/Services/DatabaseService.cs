using System;
using System.IO;
using System.Data;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Web.Services
{
    public interface IDatabaseService
    {
        IDbConnection CreateConnection();
        void InitializeDatabase();
    }

    public class DatabaseService : IDatabaseService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DatabaseService> _logger;
        private string _connectionString;
        private string _dbFilePath;

        public DatabaseService(IConfiguration configuration, ILogger<DatabaseService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");

            try
            {
                // Try to parse as SQLite connection string
                var builder = new SqliteConnectionStringBuilder(_connectionString);
                _dbFilePath = builder.DataSource;
            }
            catch (Exception ex)
            {
                // Fallback for when the configuration might still contain SQL Server string (e.g. from User Secrets)
                _logger.LogWarning(ex, "Failed to parse connection string as SQLite. It might be a legacy SQL Server string. Falling back to default 'Data Source=Coletados.db'.");
                _connectionString = "Data Source=Coletados.db";
                var builder = new SqliteConnectionStringBuilder(_connectionString);
                _dbFilePath = builder.DataSource;
            }
        }

        public IDbConnection CreateConnection()
        {
            return new SqliteConnection(_connectionString);
        }

        public void InitializeDatabase()
        {
            if (!File.Exists(_dbFilePath))
            {
                _logger.LogInformation("Database file not found. Creating new database at {Path}", _dbFilePath);
                try
                {
                    // Ensure directory exists
                    var directory = Path.GetDirectoryName(_dbFilePath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    using (var connection = new SqliteConnection(_connectionString))
                    {
                        connection.Open();

                        var schemaPath = Path.Combine(AppContext.BaseDirectory, "schema_sqlite.sql");
                        // Fallback to project root if running from VS/Code
                        if (!File.Exists(schemaPath))
                        {
                            schemaPath = Path.Combine(Directory.GetCurrentDirectory(), "schema_sqlite.sql");
                        }

                        if (File.Exists(schemaPath))
                        {
                            var schemaSql = File.ReadAllText(schemaPath);
                            using (var command = connection.CreateCommand())
                            {
                                command.CommandText = schemaSql;
                                command.ExecuteNonQuery();
                            }
                            _logger.LogInformation("Database initialized successfully with schema.");
                        }
                        else
                        {
                            _logger.LogError("Schema file not found at {Path}. Database created but tables missing.", schemaPath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize database.");
                    throw;
                }
            }
            else
            {
                 _logger.LogInformation("Database file found at {Path}.", _dbFilePath);
            }
        }
    }
}
