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

                        "ALTER TABLE ProgramasInstalados ADD COLUMN PacoteId TEXT;",
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

                    // Create table HistoricoCPU and normalized tables if they don't exist yet
                    try
                    {
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = @"
                                CREATE TABLE IF NOT EXISTS HistoricoCPU (
                                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                    ComputadorMAC TEXT NOT NULL,
                                    Consumo REAL NOT NULL,
                                    DataColeta TEXT NOT NULL,
                                    FOREIGN KEY (ComputadorMAC) REFERENCES Computadores(MAC) ON DELETE CASCADE
                                );

                                CREATE TABLE IF NOT EXISTS SmartphoneIMEIs (
                                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                    SmartphoneId INTEGER NOT NULL,
                                    IMEI TEXT NOT NULL,
                                    Ordem INTEGER NOT NULL DEFAULT 1,
                                    FOREIGN KEY (SmartphoneId) REFERENCES Smartphones(Id) ON DELETE CASCADE
                                );

                                CREATE TABLE IF NOT EXISTS ComputadorDiscos (
                                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                    ComputadorMAC TEXT NOT NULL,
                                    Letra TEXT NOT NULL,
                                    TotalGB TEXT,
                                    LivreGB TEXT,
                                    FOREIGN KEY (ComputadorMAC) REFERENCES Computadores(MAC) ON DELETE CASCADE
                                );

                                CREATE TABLE IF NOT EXISTS ColaboradorCredenciais (
                                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                    ColaboradorCPF TEXT NOT NULL,
                                    Sistema TEXT NOT NULL,
                                    UsuarioSistema TEXT,
                                    SenhaSistema TEXT,
                                    FOREIGN KEY (ColaboradorCPF) REFERENCES Colaboradores(CPF) ON DELETE CASCADE
                                );

                                CREATE TABLE IF NOT EXISTS ColaboradorTelefones (
                                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                    ColaboradorCPF TEXT NOT NULL,
                                    Tipo TEXT NOT NULL,
                                    Numero TEXT NOT NULL,
                                    FOREIGN KEY (ColaboradorCPF) REFERENCES Colaboradores(CPF) ON DELETE CASCADE
                                );";
                            command.ExecuteNonQuery();
                        }

                        // Migração de dados legados para tabelas normalizadas 1FN
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = @"
                                -- Migrar IMEIs de Smartphones
                                INSERT INTO SmartphoneIMEIs (SmartphoneId, IMEI, Ordem)
                                SELECT Id, IMEI1, 1 FROM Smartphones WHERE IMEI1 IS NOT NULL AND IMEI1 != ''
                                AND NOT EXISTS (SELECT 1 FROM SmartphoneIMEIs WHERE SmartphoneId = Smartphones.Id AND IMEI = Smartphones.IMEI1);

                                INSERT INTO SmartphoneIMEIs (SmartphoneId, IMEI, Ordem)
                                SELECT Id, IMEI2, 2 FROM Smartphones WHERE IMEI2 IS NOT NULL AND IMEI2 != ''
                                AND NOT EXISTS (SELECT 1 FROM SmartphoneIMEIs WHERE SmartphoneId = Smartphones.Id AND IMEI = Smartphones.IMEI2);

                                -- Migrar Discos de Computadores
                                INSERT INTO ComputadorDiscos (ComputadorMAC, Letra, TotalGB, LivreGB)
                                SELECT MAC, COALESCE(ArmazenamentoC, 'C:'), ArmazenamentoCTotal, ArmazenamentoCLivre 
                                FROM Computadores WHERE (ArmazenamentoCTotal IS NOT NULL OR ArmazenamentoC IS NOT NULL)
                                AND NOT EXISTS (SELECT 1 FROM ComputadorDiscos WHERE ComputadorMAC = Computadores.MAC AND Letra = COALESCE(Computadores.ArmazenamentoC, 'C:'));

                                INSERT INTO ComputadorDiscos (ComputadorMAC, Letra, TotalGB, LivreGB)
                                SELECT MAC, COALESCE(ArmazenamentoD, 'D:'), ArmazenamentoDTotal, ArmazenamentoDLivre 
                                FROM Computadores WHERE (ArmazenamentoDTotal IS NOT NULL OR ArmazenamentoD IS NOT NULL)
                                AND NOT EXISTS (SELECT 1 FROM ComputadorDiscos WHERE ComputadorMAC = Computadores.MAC AND Letra = COALESCE(Computadores.ArmazenamentoD, 'D:'));

                                -- Migrar Credenciais de Colaboradores
                                INSERT INTO ColaboradorCredenciais (ColaboradorCPF, Sistema, UsuarioSistema, SenhaSistema)
                                SELECT CPF, 'Email', Email, SenhaEmail FROM Colaboradores WHERE SenhaEmail IS NOT NULL AND SenhaEmail != ''
                                AND NOT EXISTS (SELECT 1 FROM ColaboradorCredenciais WHERE ColaboradorCPF = Colaboradores.CPF AND Sistema = 'Email');

                                INSERT INTO ColaboradorCredenciais (ColaboradorCPF, Sistema, UsuarioSistema, SenhaSistema)
                                SELECT CPF, 'Teams', Teams, SenhaTeams FROM Colaboradores WHERE SenhaTeams IS NOT NULL AND SenhaTeams != ''
                                AND NOT EXISTS (SELECT 1 FROM ColaboradorCredenciais WHERE ColaboradorCPF = Colaboradores.CPF AND Sistema = 'Teams');

                                INSERT INTO ColaboradorCredenciais (ColaboradorCPF, Sistema, UsuarioSistema, SenhaSistema)
                                SELECT CPF, 'EDespacho', EDespacho, SenhaEDespacho FROM Colaboradores WHERE SenhaEDespacho IS NOT NULL AND SenhaEDespacho != ''
                                AND NOT EXISTS (SELECT 1 FROM ColaboradorCredenciais WHERE ColaboradorCPF = Colaboradores.CPF AND Sistema = 'EDespacho');

                                INSERT INTO ColaboradorCredenciais (ColaboradorCPF, Sistema, UsuarioSistema, SenhaSistema)
                                SELECT CPF, 'Genius', Genius, SenhaGenius FROM Colaboradores WHERE SenhaGenius IS NOT NULL AND SenhaGenius != ''
                                AND NOT EXISTS (SELECT 1 FROM ColaboradorCredenciais WHERE ColaboradorCPF = Colaboradores.CPF AND Sistema = 'Genius');

                                INSERT INTO ColaboradorCredenciais (ColaboradorCPF, Sistema, UsuarioSistema, SenhaSistema)
                                SELECT CPF, 'Ibrooker', Ibrooker, SenhaIbrooker FROM Colaboradores WHERE SenhaIbrooker IS NOT NULL AND SenhaIbrooker != ''
                                AND NOT EXISTS (SELECT 1 FROM ColaboradorCredenciais WHERE ColaboradorCPF = Colaboradores.CPF AND Sistema = 'Ibrooker');

                                INSERT INTO ColaboradorCredenciais (ColaboradorCPF, Sistema, UsuarioSistema, SenhaSistema)
                                SELECT CPF, 'Adicional', Adicional, SenhaAdicional FROM Colaboradores WHERE SenhaAdicional IS NOT NULL AND SenhaAdicional != ''
                                AND NOT EXISTS (SELECT 1 FROM ColaboradorCredenciais WHERE ColaboradorCPF = Colaboradores.CPF AND Sistema = 'Adicional');

                                -- Migrar Telefones de Colaboradores
                                INSERT INTO ColaboradorTelefones (ColaboradorCPF, Tipo, Numero)
                                SELECT CPF, 'Smartphone', Smartphone FROM Colaboradores WHERE Smartphone IS NOT NULL AND Smartphone != ''
                                AND NOT EXISTS (SELECT 1 FROM ColaboradorTelefones WHERE ColaboradorCPF = Colaboradores.CPF AND Tipo = 'Smartphone');

                                INSERT INTO ColaboradorTelefones (ColaboradorCPF, Tipo, Numero)
                                SELECT CPF, 'TelefoneFixo', TelefoneFixo FROM Colaboradores WHERE TelefoneFixo IS NOT NULL AND TelefoneFixo != ''
                                AND NOT EXISTS (SELECT 1 FROM ColaboradorTelefones WHERE ColaboradorCPF = Colaboradores.CPF AND Tipo = 'TelefoneFixo');

                                INSERT INTO ColaboradorTelefones (ColaboradorCPF, Tipo, Numero)
                                SELECT CPF, 'Ramal', Ramal FROM Colaboradores WHERE Ramal IS NOT NULL AND Ramal != ''
                                AND NOT EXISTS (SELECT 1 FROM ColaboradorTelefones WHERE ColaboradorCPF = Colaboradores.CPF AND Tipo = 'Ramal');
                            ";
                            command.ExecuteNonQuery();
                        }

                        // Tentar remover colunas legadas se existirem
                        var dropColumns = new[]
                        {
                            "ALTER TABLE Smartphones DROP COLUMN IMEI1;",
                            "ALTER TABLE Smartphones DROP COLUMN IMEI2;",
                            "ALTER TABLE Computadores DROP COLUMN ArmazenamentoC;",
                            "ALTER TABLE Computadores DROP COLUMN ArmazenamentoCTotal;",
                            "ALTER TABLE Computadores DROP COLUMN ArmazenamentoCLivre;",
                            "ALTER TABLE Computadores DROP COLUMN ArmazenamentoD;",
                            "ALTER TABLE Computadores DROP COLUMN ArmazenamentoDTotal;",
                            "ALTER TABLE Computadores DROP COLUMN ArmazenamentoDLivre;",
                            "ALTER TABLE Colaboradores DROP COLUMN SenhaEmail;",
                            "ALTER TABLE Colaboradores DROP COLUMN Teams;",
                            "ALTER TABLE Colaboradores DROP COLUMN SenhaTeams;",
                            "ALTER TABLE Colaboradores DROP COLUMN EDespacho;",
                            "ALTER TABLE Colaboradores DROP COLUMN SenhaEDespacho;",
                            "ALTER TABLE Colaboradores DROP COLUMN Genius;",
                            "ALTER TABLE Colaboradores DROP COLUMN SenhaGenius;",
                            "ALTER TABLE Colaboradores DROP COLUMN Ibrooker;",
                            "ALTER TABLE Colaboradores DROP COLUMN SenhaIbrooker;",
                            "ALTER TABLE Colaboradores DROP COLUMN Adicional;",
                            "ALTER TABLE Colaboradores DROP COLUMN SenhaAdicional;",
                            "ALTER TABLE Colaboradores DROP COLUMN Smartphone;",
                            "ALTER TABLE Colaboradores DROP COLUMN TelefoneFixo;",
                            "ALTER TABLE Colaboradores DROP COLUMN Ramal;"
                        };

                        foreach (var dropStmt in dropColumns)
                        {
                            try
                            {
                                using (var cmd = connection.CreateCommand())
                                {
                                    cmd.CommandText = dropStmt;
                                    cmd.ExecuteNonQuery();
                                }
                            }
                            catch
                            {
                                // Safe to ignore if column is already dropped or not supported
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error creating normalized tables or migrating data.");
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
