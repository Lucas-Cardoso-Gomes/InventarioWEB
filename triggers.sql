-- Trigger for Colaboradores table
CREATE TRIGGER trg_Colaboradores_Log
ON Colaboradores
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    DECLARE @ActionType NVARCHAR(50);
    IF EXISTS (SELECT * FROM inserted) AND EXISTS (SELECT * FROM deleted)
        SET @ActionType = 'Update';
    ELSE IF EXISTS (SELECT * FROM inserted)
        SET @ActionType = 'Create';
    ELSE
        SET @ActionType = 'Delete';

    INSERT INTO dbo.PersistentLogs (Timestamp, EntityType, ActionType, PerformedBy, Details)
    SELECT GETDATE(), 'Colaborador', @ActionType, SUSER_SNAME(),
           COALESCE('CPF: ' + i.CPF, 'CPF: ' + d.CPF)
    FROM inserted i FULL OUTER JOIN deleted d ON i.CPF = d.CPF;
END;
GO

-- Trigger for Usuarios table
CREATE TRIGGER trg_Usuarios_Log
ON Usuarios
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    DECLARE @ActionType NVARCHAR(50);
    IF EXISTS (SELECT * FROM inserted) AND EXISTS (SELECT * FROM deleted)
        SET @ActionType = 'Update';
    ELSE IF EXISTS (SELECT * FROM inserted)
        SET @ActionType = 'Create';
    ELSE
        SET @ActionType = 'Delete';

    INSERT INTO dbo.PersistentLogs (Timestamp, EntityType, ActionType, PerformedBy, Details)
    SELECT GETDATE(), 'Usuario', @ActionType, SUSER_SNAME(),
           'ID: ' + CAST(COALESCE(i.Id, d.Id) AS NVARCHAR(10))
    FROM inserted i FULL OUTER JOIN deleted d ON i.Id = d.Id;
END;
GO

-- Trigger for Computadores table
CREATE TRIGGER trg_Computadores_Log
ON Computadores
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    DECLARE @ActionType NVARCHAR(50);
    IF EXISTS (SELECT * FROM inserted) AND EXISTS (SELECT * FROM deleted)
        SET @ActionType = 'Update';
    ELSE IF EXISTS (SELECT * FROM inserted)
        SET @ActionType = 'Create';
    ELSE
        SET @ActionType = 'Delete';

    INSERT INTO dbo.PersistentLogs (Timestamp, EntityType, ActionType, PerformedBy, Details)
    SELECT GETDATE(), 'Computador', @ActionType, SUSER_SNAME(),
           'MAC: ' + COALESCE(i.MAC, d.MAC)
    FROM inserted i FULL OUTER JOIN deleted d ON i.MAC = d.MAC;
END;
GO

-- Trigger for Monitores table
CREATE TRIGGER trg_Monitores_Log
ON Monitores
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    DECLARE @ActionType NVARCHAR(50);
    IF EXISTS (SELECT * FROM inserted) AND EXISTS (SELECT * FROM deleted)
        SET @ActionType = 'Update';
    ELSE IF EXISTS (SELECT * FROM inserted)
        SET @ActionType = 'Create';
    ELSE
        SET @ActionType = 'Delete';

    INSERT INTO dbo.PersistentLogs (Timestamp, EntityType, ActionType, PerformedBy, Details)
    SELECT GETDATE(), 'Monitor', @ActionType, SUSER_SNAME(),
           'PartNumber: ' + COALESCE(i.PartNumber, d.PartNumber)
    FROM inserted i FULL OUTER JOIN deleted d ON i.PartNumber = d.PartNumber;
END;
GO

-- Trigger for Perifericos table
CREATE TRIGGER trg_Perifericos_Log
ON Perifericos
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    DECLARE @ActionType NVARCHAR(50);
    IF EXISTS (SELECT * FROM inserted) AND EXISTS (SELECT * FROM deleted)
        SET @ActionType = 'Update';
    ELSE IF EXISTS (SELECT * FROM inserted)
        SET @ActionType = 'Create';
    ELSE
        SET @ActionType = 'Delete';

    INSERT INTO dbo.PersistentLogs (Timestamp, EntityType, ActionType, PerformedBy, Details)
    SELECT GETDATE(), 'Periferico', @ActionType, SUSER_SNAME(),
           'PartNumber: ' + COALESCE(i.PartNumber, d.PartNumber)
    FROM inserted i FULL OUTER JOIN deleted d ON i.PartNumber = d.PartNumber;
END;
GO

-- Trigger for Manutencoes table
CREATE TRIGGER trg_Manutencoes_Log
ON Manutencoes
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    DECLARE @ActionType NVARCHAR(50);
    IF EXISTS (SELECT * FROM inserted) AND EXISTS (SELECT * FROM deleted)
        SET @ActionType = 'Update';
    ELSE IF EXISTS (SELECT * FROM inserted)
        SET @ActionType = 'Create';
    ELSE
        SET @ActionType = 'Delete';

    INSERT INTO dbo.PersistentLogs (Timestamp, EntityType, ActionType, PerformedBy, Details)
    SELECT GETDATE(), 'Manutencao', @ActionType, SUSER_SNAME(),
           'ID: ' + CAST(COALESCE(i.Id, d.Id) AS NVARCHAR(10))
    FROM inserted i FULL OUTER JOIN deleted d ON i.Id = d.Id;
END;
GO

-- Trigger for Rede table
CREATE TRIGGER trg_Rede_Log
ON Rede
AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    DECLARE @ActionType NVARCHAR(50);
    IF EXISTS (SELECT * FROM inserted) AND EXISTS (SELECT * FROM deleted)
        SET @ActionType = 'Update';
    ELSE IF EXISTS (SELECT * FROM inserted)
        SET @ActionType = 'Create';
    ELSE
        SET @ActionType = 'Delete';

    INSERT INTO dbo.PersistentLogs (Timestamp, EntityType, ActionType, PerformedBy, Details)
    SELECT GETDATE(), 'Rede', @ActionType, SUSER_SNAME(),
           'ID: ' + CAST(COALESCE(i.Id, d.Id) AS NVARCHAR(10))
    FROM inserted i FULL OUTER JOIN deleted d ON i.Id = d.Id;
END;
GO
