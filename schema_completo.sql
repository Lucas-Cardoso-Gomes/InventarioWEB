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
GO

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
GO

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
    DataColeta DATETIME,
    PartNumber VARCHAR(255)
);
GO

CREATE TABLE Monitores (
    PartNumber NVARCHAR(50) PRIMARY KEY,
    ColaboradorCPF NVARCHAR(14) FOREIGN KEY REFERENCES Colaboradores(CPF),
    Marca NVARCHAR(50),
    Modelo NVARCHAR(50) NOT NULL,
    Tamanho NVARCHAR(20) NOT NULL
);
GO

CREATE TABLE Perifericos (
    PartNumber NVARCHAR(50) PRIMARY KEY,
    ColaboradorCPF NVARCHAR(14) FOREIGN KEY REFERENCES Colaboradores(CPF),
    Tipo NVARCHAR(50) NOT NULL,
    DataEntrega DATETIME
);
GO

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
GO

CREATE TABLE PersistentLogs (
    Id INT PRIMARY KEY IDENTITY,
    Timestamp DATETIME NOT NULL,
    EntityType NVARCHAR(50) NOT NULL,
    ActionType NVARCHAR(50) NOT NULL,
    PerformedBy NVARCHAR(255) NOT NULL,
    Details NVARCHAR(MAX)
);
GO

CREATE TABLE Logs (
    Id INT PRIMARY KEY IDENTITY,
    Timestamp DATETIME NOT NULL,
    Level NVARCHAR(10) NOT NULL,
    Message NVARCHAR(MAX) NOT NULL,
    Source NVARCHAR(50)
);
GO

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
GO

CREATE TABLE ChamadoConversas (
    ID INT PRIMARY KEY IDENTITY,
    ChamadoID INT NOT NULL,
    UsuarioCPF NVARCHAR(14) NOT NULL,
    Mensagem NVARCHAR(MAX) NOT NULL,
    DataCriacao DATETIME NOT NULL,
    CONSTRAINT FK_ChamadoConversas_Chamado FOREIGN KEY (ChamadoID) REFERENCES Chamados(ID) ON DELETE CASCADE,
    CONSTRAINT FK_ChamadoConversas_Usuario FOREIGN KEY (UsuarioCPF) REFERENCES Colaboradores(CPF)
);
GO

CREATE TABLE ChamadoAnexos (
    ID INT PRIMARY KEY IDENTITY,
    ChamadoID INT NOT NULL,
    NomeArquivo NVARCHAR(255) NOT NULL,
    CaminhoArquivo NVARCHAR(1024) NOT NULL,
    DataUpload DATETIME NOT NULL,
    CONSTRAINT FK_ChamadoAnexos_Chamado FOREIGN KEY (ChamadoID) REFERENCES Chamados(ID) ON DELETE CASCADE
);
GO

CREATE TABLE Rede (
    Id INT PRIMARY KEY IDENTITY,
    Tipo NVARCHAR(50) NOT NULL,
    IP NVARCHAR(45) NOT NULL,
    MAC NVARCHAR(17),
    Nome NVARCHAR(100) NOT NULL,
    DataInclusao DATETIME NOT NULL,
    DataAlteracao DATETIME,
    Observacao NVARCHAR(MAX)
);
GO

select * from rede;
GO

INSERT INTO Usuarios (Nome, Login, PasswordHash, Role, IsCoordinator) VALUES ('Admin', 'Admin', 'Admin', 'Admin', 0);
GO

-- Trigger for Colaboradores table
ALTER TRIGGER trg_Colaboradores_Log
ON Colaboradores
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    DECLARE @ActionType NVARCHAR(50);
    DECLARE @Details NVARCHAR(MAX);

    IF EXISTS (SELECT * FROM inserted) AND EXISTS (SELECT * FROM deleted)
    BEGIN
        SET @ActionType = 'Update';
        SELECT @Details = 
            (SELECT 
                (SELECT * FROM deleted FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) AS OldValues,
                (SELECT * FROM inserted FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) AS NewValues
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    END
    ELSE IF EXISTS (SELECT * FROM inserted)
    BEGIN
        SET @ActionType = 'Create';
        SELECT @Details = (SELECT * FROM inserted FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    END
    ELSE
    BEGIN
        SET @ActionType = 'Delete';
        SELECT @Details = (SELECT * FROM deleted FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    END

    INSERT INTO dbo.PersistentLogs (Timestamp, EntityType, ActionType, PerformedBy, Details)
    VALUES (GETDATE(), 'Colaborador', @ActionType, SUSER_SNAME(), @Details);
END;
GO

-- Trigger for Usuarios table
ALTER TRIGGER trg_Usuarios_Log
ON Usuarios
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    DECLARE @ActionType NVARCHAR(50);
    DECLARE @Details NVARCHAR(MAX);

    IF EXISTS (SELECT * FROM inserted) AND EXISTS (SELECT * FROM deleted)
    BEGIN
        SET @ActionType = 'Update';
        SELECT @Details = 
            (SELECT 
                (SELECT * FROM deleted FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) AS OldValues,
                (SELECT * FROM inserted FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) AS NewValues
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    END
    ELSE IF EXISTS (SELECT * FROM inserted)
    BEGIN
        SET @ActionType = 'Create';
        SELECT @Details = (SELECT * FROM inserted FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    END
    ELSE
    BEGIN
        SET @ActionType = 'Delete';
        SELECT @Details = (SELECT * FROM deleted FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    END

    INSERT INTO dbo.PersistentLogs (Timestamp, EntityType, ActionType, PerformedBy, Details)
    VALUES (GETDATE(), 'Usuario', @ActionType, SUSER_SNAME(), @Details);
END;
GO

-- Trigger for Computadores table
ALTER TRIGGER trg_Computadores_Log
ON Computadores
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    DECLARE @ActionType NVARCHAR(50);
    DECLARE @Details NVARCHAR(MAX);

    IF EXISTS (SELECT * FROM inserted) AND EXISTS (SELECT * FROM deleted)
    BEGIN
        SET @ActionType = 'Update';
        SELECT @Details = 
            (SELECT 
                (SELECT * FROM deleted FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) AS OldValues,
                (SELECT * FROM inserted FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) AS NewValues
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    END
    ELSE IF EXISTS (SELECT * FROM inserted)
    BEGIN
        SET @ActionType = 'Create';
        SELECT @Details = (SELECT * FROM inserted FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    END
    ELSE
    BEGIN
        SET @ActionType = 'Delete';
        SELECT @Details = (SELECT * FROM deleted FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    END

    INSERT INTO dbo.PersistentLogs (Timestamp, EntityType, ActionType, PerformedBy, Details)
    VALUES (GETDATE(), 'Computador', @ActionType, SUSER_SNAME(), @Details);
END;
GO

-- Trigger for Monitores table
ALTER TRIGGER trg_Monitores_Log
ON Monitores
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    DECLARE @ActionType NVARCHAR(50);
    DECLARE @Details NVARCHAR(MAX);

    IF EXISTS (SELECT * FROM inserted) AND EXISTS (SELECT * FROM deleted)
    BEGIN
        SET @ActionType = 'Update';
        SELECT @Details = 
            (SELECT 
                (SELECT * FROM deleted FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) AS OldValues,
                (SELECT * FROM inserted FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) AS NewValues
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    END
    ELSE IF EXISTS (SELECT * FROM inserted)
    BEGIN
        SET @ActionType = 'Create';
        SELECT @Details = (SELECT * FROM inserted FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    END
    ELSE
    BEGIN
        SET @ActionType = 'Delete';
        SELECT @Details = (SELECT * FROM deleted FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    END

    INSERT INTO dbo.PersistentLogs (Timestamp, EntityType, ActionType, PerformedBy, Details)
    VALUES (GETDATE(), 'Monitor', @ActionType, SUSER_SNAME(), @Details);
END;
GO

-- Trigger for Perifericos table
ALTER TRIGGER trg_Perifericos_Log
ON Perifericos
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    DECLARE @ActionType NVARCHAR(50);
    DECLARE @Details NVARCHAR(MAX);

    IF EXISTS (SELECT * FROM inserted) AND EXISTS (SELECT * FROM deleted)
    BEGIN
        SET @ActionType = 'Update';
        SELECT @Details = 
            (SELECT 
                (SELECT * FROM deleted FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) AS OldValues,
                (SELECT * FROM inserted FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) AS NewValues
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    END
    ELSE IF EXISTS (SELECT * FROM inserted)
    BEGIN
        SET @ActionType = 'Create';
        SELECT @Details = (SELECT * FROM inserted FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    END
    ELSE
    BEGIN
        SET @ActionType = 'Delete';
        SELECT @Details = (SELECT * FROM deleted FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    END

    INSERT INTO dbo.PersistentLogs (Timestamp, EntityType, ActionType, PerformedBy, Details)
    VALUES (GETDATE(), 'Periferico', @ActionType, SUSER_SNAME(), @Details);
END;
GO

-- Trigger for Manutencoes table
ALTER TRIGGER trg_Manutencoes_Log
ON Manutencoes
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    DECLARE @ActionType NVARCHAR(50);
    DECLARE @Details NVARCHAR(MAX);

    IF EXISTS (SELECT * FROM inserted) AND EXISTS (SELECT * FROM deleted)
    BEGIN
        SET @ActionType = 'Update';
        SELECT @Details = 
            (SELECT 
                (SELECT * FROM deleted FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) AS OldValues,
                (SELECT * FROM inserted FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) AS NewValues
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    END
    ELSE IF EXISTS (SELECT * FROM inserted)
    BEGIN
        SET @ActionType = 'Create';
        SELECT @Details = (SELECT * FROM inserted FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    END
    ELSE
    BEGIN
        SET @ActionType = 'Delete';
        SELECT @Details = (SELECT * FROM deleted FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    END

    INSERT INTO dbo.PersistentLogs (Timestamp, EntityType, ActionType, PerformedBy, Details)
    VALUES (GETDATE(), 'Manutencao', @ActionType, SUSER_SNAME(), @Details);
END;
GO

-- Trigger for Rede table
ALTER TRIGGER trg_Rede_Log
ON Rede
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    DECLARE @ActionType NVARCHAR(50);
    DECLARE @Details NVARCHAR(MAX);

    IF EXISTS (SELECT * FROM inserted) AND EXISTS (SELECT * FROM deleted)
    BEGIN
        SET @ActionType = 'Update';
        SELECT @Details = 
            (SELECT 
                (SELECT * FROM deleted FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) AS OldValues,
                (SELECT * FROM inserted FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) AS NewValues
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    END
    ELSE IF EXISTS (SELECT * FROM inserted)
    BEGIN
        SET @ActionType = 'Create';
        SELECT @Details = (SELECT * FROM inserted FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    END
    ELSE
    BEGIN
        SET @ActionType = 'Delete';
        SELECT @Details = (SELECT * FROM deleted FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    END

    INSERT INTO dbo.PersistentLogs (Timestamp, EntityType, ActionType, PerformedBy, Details)
    VALUES (GETDATE(), 'Rede', @ActionType, SUSER_SNAME(), @Details);
END;
GO

CREATE TABLE Smartphones (
    Id INT PRIMARY KEY IDENTITY,
    Modelo NVARCHAR(100) NOT NULL,
    IMEI1 NVARCHAR(15) NOT NULL,
    IMEI2 NVARCHAR(15),
    Usuario NVARCHAR(100),
    Filial NVARCHAR(100),
    DataCriacao DATETIME NOT NULL,
    DataAlteracao DATETIME,
    ContaGoogle NVARCHAR(100),
    SenhaGoogle NVARCHAR(100)
);
GO

-- Trigger for Smartphones table
CREATE TRIGGER trg_Smartphones_Log
ON Smartphones
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    DECLARE @ActionType NVARCHAR(50);
    DECLARE @Details NVARCHAR(MAX);

    IF EXISTS (SELECT * FROM inserted) AND EXISTS (SELECT * FROM deleted)
    BEGIN
        SET @ActionType = 'Update';
        SELECT @Details = 
            (SELECT 
                (SELECT * FROM deleted FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) AS OldValues,
                (SELECT * FROM inserted FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) AS NewValues
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    END
    ELSE IF EXISTS (SELECT * FROM inserted)
    BEGIN
        SET @ActionType = 'Create';
        SELECT @Details = (SELECT * FROM inserted FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    END
    ELSE
    BEGIN
        SET @ActionType = 'Delete';
        SELECT @Details = (SELECT * FROM deleted FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);
    END

    INSERT INTO dbo.PersistentLogs (Timestamp, EntityType, ActionType, PerformedBy, Details)
    VALUES (GETDATE(), 'Smartphone', @ActionType, SUSER_SNAME(), @Details);
END;
GO