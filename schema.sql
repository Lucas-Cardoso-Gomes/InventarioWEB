-- schema.sql

CREATE TABLE Usuarios (
    Id INT PRIMARY KEY IDENTITY,
    Nome NVARCHAR(100) NOT NULL,
    Login NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(MAX) NOT NULL,
    Role NVARCHAR(50) NOT NULL,
    CPF NVARCHAR(14),
    Email NVARCHAR(100),
    SenhaEmail NVARCHAR(100),
    Teams NVARCHAR(100),
    SenhaTeams NVARCHAR(100),
    EDespacho NVARCHAR(100),
    SenhaEDespacho NVARCHAR(100),
    Genius NVARCHAR(100),
    SenhaGenius NVARCHAR(100),
    Ibrooker NVARCHAR(100),
    SenhaIbrooker NVARCHAR(100),
    Adicional NVARCHAR(100),
    SenhaAdicional NVARCHAR(100),
    Setor NVARCHAR(100),
    Smartphone NVARCHAR(100),
    TelefoneFixo NVARCHAR(100),
    Ramal NVARCHAR(100),
    Alarme NVARCHAR(100),
    Videoporteiro NVARCHAR(100),
    Obs NVARCHAR(MAX),
    DataInclusao DATETIME,
    DataAlteracao DATETIME,
    CoordenadorId INT,
    CONSTRAINT FK_Usuarios_Coordenador FOREIGN KEY (CoordenadorId) REFERENCES Usuarios(Id)
);

CREATE TABLE Computadores (
    MAC NVARCHAR(50) PRIMARY KEY,
    IP NVARCHAR(50),
    UserId INT,
    Hostname NVARCHAR(100) NOT NULL,
    Fabricante NVARCHAR(100),
    Processador NVARCHAR(100),
    ProcessadorFabricante NVARCHAR(100),
    ProcessadorCore NVARCHAR(50),
    ProcessadorThread NVARCHAR(50),
    ProcessadorClock NVARCHAR(50),
    Ram NVARCHAR(50),
    RamTipo NVARCHAR(50),
    RamVelocidade NVARCHAR(50),
    RamVoltagem NVARCHAR(50),
    RamPorModule NVARCHAR(50),
    ArmazenamentoC NVARCHAR(100),
    ArmazenamentoCTotal NVARCHAR(100),
    ArmazenamentoCLivre NVARCHAR(100),
    ArmazenamentoD NVARCHAR(100),
    ArmazenamentoDTotal NVARCHAR(100),
    ArmazenamentoDLivre NVARCHAR(100),
    ConsumoCPU NVARCHAR(50),
    SO NVARCHAR(100),
    DataColeta DATETIME,
    CONSTRAINT FK_Computadores_Usuarios FOREIGN KEY (UserId) REFERENCES Usuarios(Id)
);

CREATE TABLE Monitores (
    PartNumber NVARCHAR(50) PRIMARY KEY,
    UserId INT,
    Marca NVARCHAR(50),
    Modelo NVARCHAR(50) NOT NULL,
    Tamanho NVARCHAR(20),
    CONSTRAINT FK_Monitores_Usuarios FOREIGN KEY (UserId) REFERENCES Usuarios(Id)
);

CREATE TABLE Perifericos (
    ID INT PRIMARY KEY IDENTITY,
    UserId INT,
    Tipo NVARCHAR(50) NOT NULL,
    DataEntrega DATETIME,
    PartNumber NVARCHAR(50),
    CONSTRAINT FK_Perifericos_Usuarios FOREIGN KEY (UserId) REFERENCES Usuarios(Id)
);

CREATE TABLE Manutencoes (
    Id INT PRIMARY KEY IDENTITY,
    ComputadorMAC NVARCHAR(50) NOT NULL,
    Data DATETIME NOT NULL,
    Descricao NVARCHAR(MAX) NOT NULL,
    Custo DECIMAL(18, 2),
    CONSTRAINT FK_Manutencoes_Computadores FOREIGN KEY (ComputadorMAC) REFERENCES Computadores(MAC)
);

CREATE TABLE Logs (
    Id INT PRIMARY KEY IDENTITY,
    Timestamp DATETIME NOT NULL,
    Level NVARCHAR(50) NOT NULL,
    Message NVARCHAR(MAX) NOT NULL,
    Exception NVARCHAR(MAX)
);

CREATE TABLE PersistentLogs (
    Id INT PRIMARY KEY IDENTITY,
    Timestamp DATETIME NOT NULL,
    EntityType NVARCHAR(100),
    ActionType NVARCHAR(100),
    PerformedBy NVARCHAR(100),
    Details NVARCHAR(MAX)
);
