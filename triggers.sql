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
