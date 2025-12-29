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
        IDbConnection CreateLogsConnection();
        void InitializeDatabase();
    }

    public class DatabaseService : IDatabaseService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<DatabaseService> _logger;
        private string _connectionString;
        private string _dbFilePath;
        private string _logsConnectionString;
        private string _logsDbFilePath;

        public DatabaseService(IConfiguration configuration, ILogger<DatabaseService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
            _logsConnectionString = _configuration.GetConnectionString("LogsConnection");

            try
            {
                // Try to parse as SQLite connection string
                var builder = new SqliteConnectionStringBuilder(_connectionString);
                _dbFilePath = builder.DataSource;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse main connection string as SQLite. Falling back to default 'Data Source=Coletados.db'.");
                _connectionString = "Data Source=Coletados.db";
                var builder = new SqliteConnectionStringBuilder(_connectionString);
                _dbFilePath = builder.DataSource;
            }

            try
            {
                if (string.IsNullOrEmpty(_logsConnectionString))
                {
                    _logsConnectionString = "Data Source=ColetadosLogs.db";
                }
                var builderLogs = new SqliteConnectionStringBuilder(_logsConnectionString);
                _logsDbFilePath = builderLogs.DataSource;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse logs connection string. Falling back to default 'Data Source=ColetadosLogs.db'.");
                _logsConnectionString = "Data Source=ColetadosLogs.db";
                var builderLogs = new SqliteConnectionStringBuilder(_logsConnectionString);
                _logsDbFilePath = builderLogs.DataSource;
            }
        }

        public IDbConnection CreateConnection()
        {
            return new SqliteConnection(_connectionString);
        }

        public IDbConnection CreateLogsConnection()
        {
            return new SqliteConnection(_logsConnectionString);
        }

        public void InitializeDatabase()
        {
            InitializeMainDatabase();
            InitializeLogsDatabase();
        }

        private void InitializeMainDatabase()
        {
            if (!File.Exists(_dbFilePath))
            {
                _logger.LogInformation("Main Database file not found. Creating new database at {Path}", _dbFilePath);
                try
                {
                    var directory = Path.GetDirectoryName(_dbFilePath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    using (var connection = new SqliteConnection(_connectionString))
                    {
                        connection.Open();

                        var schemaPath = Path.Combine(AppContext.BaseDirectory, "schema_main.sql");
                        if (!File.Exists(schemaPath))
                        {
                            schemaPath = Path.Combine(Directory.GetCurrentDirectory(), "schema_main.sql");
                        }

                        if (File.Exists(schemaPath))
                        {
                            var schemaSql = File.ReadAllText(schemaPath);
                            using (var command = connection.CreateCommand())
                            {
                                command.CommandText = schemaSql;
                                command.ExecuteNonQuery();
                            }
                            _logger.LogInformation("Main Database initialized successfully with schema.");
                        }
                        else
                        {
                            _logger.LogError("Main Schema file not found at {Path}. Database created but tables missing.", schemaPath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize main database.");
                    throw;
                }
            }
            else
            {
                 _logger.LogInformation("Main Database file found at {Path}.", _dbFilePath);
            }
        }

        private void InitializeLogsDatabase()
        {
            if (!File.Exists(_logsDbFilePath))
            {
                _logger.LogInformation("Logs Database file not found. Creating new database at {Path}", _logsDbFilePath);
                try
                {
                    var directory = Path.GetDirectoryName(_logsDbFilePath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    using (var connection = new SqliteConnection(_logsConnectionString))
                    {
                        connection.Open();

                        var schemaPath = Path.Combine(AppContext.BaseDirectory, "schema_logs.sql");
                        if (!File.Exists(schemaPath))
                        {
                            schemaPath = Path.Combine(Directory.GetCurrentDirectory(), "schema_logs.sql");
                        }

                        if (File.Exists(schemaPath))
                        {
                            var schemaSql = File.ReadAllText(schemaPath);
                            using (var command = connection.CreateCommand())
                            {
                                command.CommandText = schemaSql;
                                command.ExecuteNonQuery();
                            }
                            _logger.LogInformation("Logs Database initialized successfully with schema.");
                        }
                        else
                        {
                            _logger.LogError("Logs Schema file not found at {Path}. Database created but tables missing.", schemaPath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize logs database.");
                    throw;
                }
            }
            else
            {
                 _logger.LogInformation("Logs Database file found at {Path}.", _logsDbFilePath);
            }
        }
    }
}
