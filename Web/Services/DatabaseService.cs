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
            ApplySchemaUpdates();
        }

        private void ApplySchemaUpdates()
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();

                    // Safely add new columns if they don't exist
                    var columnsToAdd = new[]
                    {
                        "ALTER TABLE Rede ADD COLUMN Localizacao TEXT;",
                        "ALTER TABLE Rede ADD COLUMN Endereco TEXT;",
                        "ALTER TABLE Rede ADD COLUMN Local TEXT;",
                        "ALTER TABLE Rede ADD COLUMN DataGarantia TEXT;",

                        "ALTER TABLE Computadores ADD COLUMN DataGarantia TEXT;",
                        "ALTER TABLE Computadores ADD COLUMN Backup TEXT;",
                        "ALTER TABLE Computadores ADD COLUMN ProcessadorTemperatura TEXT;",

                        "ALTER TABLE Computadores ADD COLUMN BateriaWearLevel TEXT;",
                        "ALTER TABLE Computadores ADD COLUMN TempoAtividade TEXT;",
                        "ALTER TABLE Computadores ADD COLUMN Localizacao TEXT;",

                        "ALTER TABLE ChamadoConversas ADD COLUMN Lido INTEGER NOT NULL DEFAULT 0;",

                        "ALTER TABLE Monitores ADD COLUMN DataGarantia TEXT;",

                        "ALTER TABLE Perifericos ADD COLUMN DataGarantia TEXT;",

                        "ALTER TABLE Smartphones ADD COLUMN DataGarantia TEXT;",

                        "ALTER TABLE Usuarios ADD COLUMN IsActive INTEGER NOT NULL DEFAULT 1;",

                        "ALTER TABLE Feedbacks ADD COLUMN Protocolo TEXT;"
                    };

                    foreach (var stmt in columnsToAdd)
                    {
                        try
                        {
                            using (var command = connection.CreateCommand())
                            {
                                command.CommandText = stmt;
                                command.ExecuteNonQuery();
                            }
                        }
                        catch (SqliteException ex) when (ex.SqliteErrorCode == 1) // 1 = SQLITE_ERROR, typically "duplicate column name"
                        {
                            // Column already exists, safe to ignore
                        }
                    }

                    // Create table HistoricoTrocas if it doesn't exist yet
                    try
                    {
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = @"
                                CREATE TABLE IF NOT EXISTS HistoricoTrocas (
                                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                    EquipamentoId TEXT NOT NULL,
                                    TipoEquipamento TEXT NOT NULL,
                                    CampoAlterado TEXT NOT NULL,
                                    ValorAntigo TEXT,
                                    ValorNovo TEXT,
                                    DataAlteracao TEXT NOT NULL,
                                    Usuario TEXT NOT NULL
                                );

                                CREATE TABLE IF NOT EXISTS Feedbacks (
                                    ID INTEGER PRIMARY KEY AUTOINCREMENT,
                                    Protocolo TEXT NOT NULL UNIQUE,
                                    Assunto TEXT NOT NULL,
                                    Mensagem TEXT NOT NULL,
                                    DataCriacao TEXT NOT NULL,
                                    UsuarioCPF TEXT,
                                    Status TEXT NOT NULL DEFAULT 'Aberto' CHECK (Status IN ('Aberto', 'Em Andamento', 'Fechado')),
                                    FOREIGN KEY (UsuarioCPF) REFERENCES Colaboradores(CPF)
                                );

                                CREATE TABLE IF NOT EXISTS FeedbackConversas (
                                    ID INTEGER PRIMARY KEY AUTOINCREMENT,
                                    FeedbackID INTEGER NOT NULL,
                                    UsuarioCPF TEXT,
                                    Remetente TEXT NOT NULL,
                                    Mensagem TEXT NOT NULL,
                                    DataCriacao TEXT NOT NULL,
                                    FOREIGN KEY (FeedbackID) REFERENCES Feedbacks(ID) ON DELETE CASCADE,
                                    FOREIGN KEY (UsuarioCPF) REFERENCES Colaboradores(CPF)
                                );

                                CREATE TABLE IF NOT EXISTS ProgramasInstalados (
                                    ID INTEGER PRIMARY KEY AUTOINCREMENT,
                                    ComputadorMAC TEXT NOT NULL,
                                    Nome TEXT NOT NULL,
                                    Versao TEXT,
                                    Desenvolvedor TEXT,
                                    DataColeta TEXT NOT NULL,
                                    FOREIGN KEY (ComputadorMAC) REFERENCES Computadores(MAC) ON DELETE CASCADE
                                );";
                            command.ExecuteNonQuery();
                        }
                    }
                    catch (SqliteException ex)
                    {
                        _logger.LogWarning($"Schema update error for HistoricoTrocas (handled): {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply schema updates.");
            }
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
