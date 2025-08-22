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
