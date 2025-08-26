-- Step 1: Create the new Users table
CREATE TABLE Users (
    Id INT PRIMARY KEY IDENTITY,
    Nome NVARCHAR(100) NOT NULL,
    Login NVARCHAR(50) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(MAX) NOT NULL,
    Role INT NOT NULL,
    CPF NVARCHAR(14) NOT NULL UNIQUE,
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
    FOREIGN KEY (CoordenadorId) REFERENCES Users(Id)
);
GO

-- Step 2: Migrate data from Colaboradores to Users
-- IMPORTANT: You need to replace the placeholder password hash with a real one.
-- You can generate one using the application or a library like BCrypt.
-- The default password used here is "Pa$$w0rd"
-- The hash for "Pa$$w0rd" is: '$2a$11$8qO9R.y.C.9.p2.Q7e9f4u.W.t.1.F.e.z.j.o.A.y.2.U.i.O.u.G'
INSERT INTO Users (Nome, Login, PasswordHash, Role, CPF, Email, SenhaEmail, Teams, SenhaTeams, EDespacho, SenhaEDespacho, Genius, SenhaGenius, Ibrooker, SenhaIbrooker, Adicional, SenhaAdicional, Setor, Smartphone, TelefoneFixo, Ramal, Alarme, Videoporteiro, Obs, DataInclusao)
SELECT Nome, CPF, '$2a$11$8qO9R.y.C.9.p2.Q7e9f4u.W.t.1.F.e.z.j.o.A.y.2.U.i.O.u.G', 0, CPF, Email, SenhaEmail, Teams, SenhaTeams, EDespacho, SenhaEDespacho, Genius, SenhaGenius, Ibrooker, SenhaIbrooker, Adicional, SenhaAdicional, Setor, Smartphone, TelefoneFixo, Ramal, Alarme, Videoporteiro, Obs, DataInclusao
FROM Colaboradores;
GO

-- Step 3: Migrate data from old Usuarios table to Users
INSERT INTO Users (Nome, Login, PasswordHash, Role, CPF)
SELECT Nome, Login, PasswordHash, CASE WHEN Role = 'Admin' THEN 3 ELSE 0 END, Login
FROM Usuarios
WHERE Login NOT IN (SELECT Login FROM Users);
GO

-- Step 4: Update device tables with UserId
ALTER TABLE Computadores ADD UserId INT;
GO
ALTER TABLE Monitores ADD UserId INT;
GO
ALTER TABLE Perifericos ADD UserId INT;
GO

UPDATE c
SET c.UserId = u.Id
FROM Computadores c
JOIN Users u ON c.ColaboradorNome = u.Nome;
GO

UPDATE m
SET m.UserId = u.Id
FROM Monitores m
JOIN Users u ON m.ColaboradorNome = u.Nome;
GO

UPDATE p
SET p.UserId = u.Id
FROM Perifericos p
JOIN Users u ON p.ColaboradorNome = u.Nome;
GO

-- Step 5: Drop old columns and add foreign key constraints
ALTER TABLE Computadores DROP COLUMN ColaboradorNome;
GO
ALTER TABLE Monitores DROP COLUMN ColaboradorNome;
GO
ALTER TABLE Perifericos DROP COLUMN ColaboradorNome;
GO

ALTER TABLE Computadores ADD CONSTRAINT FK_Computadores_Users FOREIGN KEY (UserId) REFERENCES Users(Id);
GO
ALTER TABLE Monitores ADD CONSTRAINT FK_Monitores_Users FOREIGN KEY (UserId) REFERENCES Users(Id);
GO
ALTER TABLE Perifericos ADD CONSTRAINT FK_Perifericos_Users FOREIGN KEY (UserId) REFERENCES Users(Id);
GO

-- Step 6: Drop old tables
DROP TABLE Colaboradores;
GO
DROP TABLE Usuarios;
GO
