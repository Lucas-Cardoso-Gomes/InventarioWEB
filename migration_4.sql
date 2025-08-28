-- Add columns from Colaboradores to Usuarios
ALTER TABLE Usuarios ADD CPF VARCHAR(14) NULL;
ALTER TABLE Usuarios ADD Email VARCHAR(100) NULL;
ALTER TABLE Usuarios ADD SenhaEmail VARCHAR(100) NULL;
ALTER TABLE Usuarios ADD Teams VARCHAR(100) NULL;
ALTER TABLE Usuarios ADD SenhaTeams VARCHAR(100) NULL;
ALTER TABLE Usuarios ADD EDespacho VARCHAR(100) NULL;
ALTER TABLE Usuarios ADD SenhaEDespacho VARCHAR(100) NULL;
ALTER TABLE Usuarios ADD Genius VARCHAR(100) NULL;
ALTER TABLE Usuarios ADD SenhaGenius VARCHAR(100) NULL;
ALTER TABLE Usuarios ADD Ibrooker VARCHAR(100) NULL;
ALTER TABLE Usuarios ADD SenhaIbrooker VARCHAR(100) NULL;
ALTER TABLE Usuarios ADD Adicional VARCHAR(100) NULL;
ALTER TABLE Usuarios ADD SenhaAdicional VARCHAR(100) NULL;
ALTER TABLE Usuarios ADD Setor VARCHAR(100) NULL;
ALTER TABLE Usuarios ADD Smartphone VARCHAR(100) NULL;
ALTER TABLE Usuarios ADD TelefoneFixo VARCHAR(100) NULL;
ALTER TABLE Usuarios ADD Ramal VARCHAR(100) NULL;
ALTER TABLE Usuarios ADD Alarme VARCHAR(100) NULL;
ALTER TABLE Usuarios ADD Videoporteiro VARCHAR(100) NULL;
ALTER TABLE Usuarios ADD Obs NVARCHAR(MAX) NULL;
ALTER TABLE Usuarios ADD DataInclusao DATETIME NULL;
ALTER TABLE Usuarios ADD DataAlteracao DATETIME NULL;

-- Rename CoordenadorId to SupervisorId
EXEC sp_rename 'Usuarios.CoordenadorId', 'SupervisorId', 'COLUMN';

-- Copy data from Colaboradores to Usuarios
UPDATE u
SET
    u.CPF = c.CPF,
    u.Email = c.Email,
    u.SenhaEmail = c.SenhaEmail,
    u.Teams = c.Teams,
    u.SenhaTeams = c.SenhaTeams,
    u.EDespacho = c.EDespacho,
    u.SenhaEDespacho = c.SenhaEDespacho,
    u.Genius = c.Genius,
    u.SenhaGenius = c.SenhaGenius,
    u.Ibrooker = c.Ibrooker,
    u.SenhaIbrooker = c.SenhaIbrooker,
    u.Adicional = c.Adicional,
    u.SenhaAdicional = c.SenhaAdicional,
    u.Setor = c.Setor,
    u.Smartphone = c.Smartphone,
    u.TelefoneFixo = c.TelefoneFixo,
    u.Ramal = c.Ramal,
    u.Alarme = c.Alarme,
    u.Videoporteiro = c.Videoporteiro,
    u.Obs = c.Obs,
    u.DataInclusao = c.DataInclusao,
    u.DataAlteracao = c.DataAlteracao
FROM Usuarios u
JOIN Colaboradores c ON u.ColaboradorCPF = c.CPF;

-- Update SupervisorId from Coordenador name
UPDATE u
SET u.SupervisorId = (SELECT Id FROM Usuarios WHERE Nome = c.Coordenador)
FROM Usuarios u
JOIN Colaboradores c ON u.ColaboradorCPF = c.CPF
WHERE c.Coordenador IS NOT NULL;

-- Drop the Colaboradores table
DROP TABLE Colaboradores;

-- Drop the ColaboradorCPF column from Usuarios
ALTER TABLE Usuarios DROP COLUMN ColaboradorCPF;
