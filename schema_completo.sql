/*
use master
drop database Coletados;
create database Coletados;
Use Coletados;
SELECT name FROM sys.tables;
*/

CREATE TABLE Colaboradores (
    CPF NVARCHAR(14) PRIMARY KEY,
    Nome NVARCHAR(100) NOT NULL UNIQUE,
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
    Filial NVARCHAR(100),
    Setor NVARCHAR(100),
    Smartphone NVARCHAR(100),
    TelefoneFixo NVARCHAR(100),
    Ramal NVARCHAR(100),
    Alarme NVARCHAR(100),
    Videoporteiro NVARCHAR(100),
    Obs NVARCHAR(MAX),
    DataInclusao DATETIME NOT NULL,
    DataAlteracao DATETIME,
    CoordenadorCPF NVARCHAR(14),
    CONSTRAINT FK_Colaboradores_Coordenador FOREIGN KEY (CoordenadorCPF) REFERENCES Colaboradores(CPF)
);

CREATE TABLE Usuarios (
    Id INT PRIMARY KEY IDENTITY,
    Nome NVARCHAR(100) NOT NULL,
    Login NVARCHAR(50) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    Role NVARCHAR(20) NOT NULL CHECK (Role IN ('Admin', 'Coordenador', 'Colaborador', 'Diretoria/RH')),
    ColaboradorCPF NVARCHAR(14) NULL,
    IsCoordinator BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_Usuarios_Colaboradores FOREIGN KEY (ColaboradorCPF) REFERENCES Colaboradores(CPF)
);

CREATE TABLE Computadores (
    MAC NVARCHAR(17) PRIMARY KEY,
    IP NVARCHAR(45),
    ColaboradorCPF NVARCHAR(14) FOREIGN KEY REFERENCES Colaboradores(CPF),
    Hostname NVARCHAR(100) NOT NULL,
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

CREATE TABLE Monitores (
    PartNumber NVARCHAR(50) PRIMARY KEY,
    ColaboradorCPF NVARCHAR(14) FOREIGN KEY REFERENCES Colaboradores(CPF),
    Marca NVARCHAR(50),
    Modelo NVARCHAR(50) NOT NULL,
    Tamanho NVARCHAR(20) NOT NULL
);

CREATE TABLE Perifericos (
    PartNumber NVARCHAR(50) PRIMARY KEY,
    ColaboradorCPF NVARCHAR(14) FOREIGN KEY REFERENCES Colaboradores(CPF),
    Tipo NVARCHAR(50) NOT NULL,
    DataEntrega DATETIME
);

CREATE TABLE Manutencoes (
    Id INT PRIMARY KEY IDENTITY,
    ComputadorMAC NVARCHAR(17) FOREIGN KEY REFERENCES Computadores(MAC),
    MonitorPartNumber NVARCHAR(50) FOREIGN KEY REFERENCES Monitores(PartNumber),
    PerifericoPartNumber NVARCHAR(50) FOREIGN KEY REFERENCES Perifericos(PartNumber),
    DataManutencaoHardware DATETIME,
    DataManutencaoSoftware DATETIME,
    ManutencaoExterna NVARCHAR(MAX),
    Data DATETIME,
    Historico NVARCHAR(MAX)
);

CREATE TABLE PersistentLogs (
    Id INT PRIMARY KEY IDENTITY,
    Timestamp DATETIME NOT NULL,
    EntityType NVARCHAR(50) NOT NULL,
    ActionType NVARCHAR(50) NOT NULL,
    PerformedBy NVARCHAR(255) NOT NULL,
    Details NVARCHAR(MAX)
);

CREATE TABLE Logs (
    Id INT PRIMARY KEY IDENTITY,
    Timestamp DATETIME NOT NULL,
    Level NVARCHAR(10) NOT NULL,
    Message NVARCHAR(MAX) NOT NULL,
    Source NVARCHAR(50)
);

CREATE TABLE Chamados (
    ID INT PRIMARY KEY IDENTITY,
    AdminCPF NVARCHAR(14) FOREIGN KEY REFERENCES Colaboradores(CPF),
    ColaboradorCPF NVARCHAR(14) FOREIGN KEY REFERENCES Colaboradores(CPF) NOT NULL,
    Servico NVARCHAR(100) NOT NULL,
    Descricao NVARCHAR(1000) NOT NULL,
    DataAlteracao DATETIME,
    DataCriacao DATETIME NOT NULL,
    Status NVARCHAR(50) NOT NULL DEFAULT 'Aberto' CHECK (Status IN ('Aberto', 'Em Andamento', 'Fechado'))
);

CREATE TABLE Rede (
    Id INT PRIMARY KEY IDENTITY,
    Tipo NVARCHAR(50) NOT NULL,
    IP NVARCHAR(45) NOT NULL,
    MAC NVARCHAR(17),
    Nome NVARCHAR(100) NOT NULL,
    DataInclusao DATETIME NOT NULL,
    DataAlteracao DATETIME,
    Observacao NVARCHAR(MAX),
);

select * from rede;

INSERT INTO Usuarios (Nome, Login, PasswordHash, Role, IsCoordinator) VALUES ('Admin', 'Admin', 'Admin', 'Admin', 0);