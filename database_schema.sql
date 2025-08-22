-- Este arquivo documenta o esquema do banco de dados para o projeto.

-- Tabela para armazenar informações dos computadores monitorados.
CREATE TABLE Computadores (
    MAC NVARCHAR(17) PRIMARY KEY,
    IP NVARCHAR(45),
    Usuario NVARCHAR(100),
    Hostname NVARCHAR(100),
    Fabricante NVARCHAR(100),
    Processador NVARCHAR(255),
    ProcessadorFabricante NVARCHAR(100),
    ProcessadorCore NVARCHAR(10),
    ProcessadorThread NVARCHAR(10),
    ProcessadorClock NVARCHAR(50),
    Ram NVARCHAR(50),
    RamTipo NVARCHAR(50),
    RamVelocidade NVARCHAR(50),
    RamVoltagem NVARCHAR(50),
    RamPorModule NVARCHAR(50),
    ArmazenamentoC NVARCHAR(10),
    ArmazenamentoCTotal NVARCHAR(50),
    ArmazenamentoCLivre NVARCHAR(50),
    ArmazenamentoD NVARCHAR(10),
    ArmazenamentoDTotal NVARCHAR(50),
    ArmazenamentoDLivre NVARCHAR(50),
    ConsumoCPU NVARCHAR(50),
    SO NVARCHAR(255),
    DataColeta DATETIME
);

-- Tabela para armazenar logs do sistema.
CREATE TABLE Logs (
    Id INT PRIMARY KEY IDENTITY,
    Timestamp DATETIME NOT NULL,
    Level NVARCHAR(10) NOT NULL,
    Message NVARCHAR(MAX) NOT NULL,
    Source NVARCHAR(50)
);

-- Tabela para armazenar os usuários do sistema.
CREATE TABLE Usuarios (
    Id INT PRIMARY KEY IDENTITY,
    Nome NVARCHAR(100) NOT NULL,
    Login NVARCHAR(50) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    Role NVARCHAR(20) NOT NULL -- "Admin" or "Normal"
);

INSERT INTO Usuarios (Nome, Login, PasswordHash, Role) VALUES ('Admin User', 'admin', 'admin', 'Admin');