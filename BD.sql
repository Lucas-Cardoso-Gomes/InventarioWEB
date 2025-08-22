-- Adicionar a tabela Colaboradores
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
    Obs NVARCHAR(MAX),
    DataInclusao DATETIME NOT NULL,
    DataAlteracao DATETIME
);

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

-- Adicionar a tabela Monitores
CREATE TABLE Monitores (
    PartNumber NVARCHAR(50) PRIMARY KEY,
    ColaboradorNome NVARCHAR(100) FOREIGN KEY REFERENCES Colaboradores(Nome),
    Marca NVARCHAR(50),
    Modelo NVARCHAR(50),
    Tamanho NVARCHAR(20)
);

-- Adicionar a tabela Perifericos
CREATE TABLE Perifericos (
    ID INT PRIMARY KEY IDENTITY,
    ColaboradorNome NVARCHAR(100) FOREIGN KEY REFERENCES Colaboradores(Nome),
    Tipo NVARCHAR(50),
    DataEntrega DATETIME,
    PartNumber NVARCHAR(50)
);

-- Alterar a tabela Computadores para usar o nome do colaborador
ALTER TABLE Computadores
ADD ColaboradorNome NVARCHAR(100) FOREIGN KEY REFERENCES Colaboradores(Nome);

INSERT INTO Usuarios (Nome, Login, PasswordHash, Role) VALUES
 ('Admin', 'Admin', 'Admin', 'Admin'),
 ('Convidado', 'Convidado', 'Convidado', 'Normal');
-- Atualizar a role dos usuários para incluir Coordenador
-- A query exata para alterar a restrição pode variar dependendo do SQL Server.
-- Primeiro, identificamos e removemos a restrição existente.
-- O nome da restrição pode ser encontrado com: sp_helpconstraint 'Usuarios'
-- Supondo que o nome seja algo como 'CK__Usuarios__Role__...', o comando seria:
-- ALTER TABLE Usuarios DROP CONSTRAINT [NomeDaConstraint];
-- E então adicionamos a nova:
-- ALTER TABLE Usuarios ADD CONSTRAINT CK_Usuarios_Role CHECK (Role IN ('Admin', 'Coordenador', 'Normal'));

-- Por simplicidade aqui, vamos assumir que a aplicação irá lidar com os roles.
-- Apenas para documentação, o SQL seria algo como:
-- UPDATE Usuarios SET Role = 'Normal' WHERE Role = 'User';
-- E a lógica de autorização no código será a principal barreira.
