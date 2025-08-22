-- Este arquivo documenta o novo esquema do banco de dados para o projeto.
-- O acesso a dados será feito via Entity Framework Core.

-- Tabela principal para armazenar informações dos computadores monitorados.
CREATE TABLE Computadores (
    MAC NVARCHAR(17) PRIMARY KEY,
    IP NVARCHAR(45),
    Usuario NVARCHAR(100),
    Hostname NVARCHAR(100),
    Fabricante NVARCHAR(100),
    SO NVARCHAR(255),
    DataColeta DATETIME,

    -- Info Processador
    ProcessadorNome NVARCHAR(255),
    ProcessadorFabricante NVARCHAR(100),
    ProcessadorCores INT,
    ProcessadorThreads INT,
    ProcessadorClock NVARCHAR(50),

    -- Info RAM
    RamTotal NVARCHAR(50),
    RamTipo NVARCHAR(50),
    RamVelocidade NVARCHAR(50),
    RamVoltagem NVARCHAR(50),
    RamPorModulo NVARCHAR(50),

    -- Info Consumo
    ConsumoCPU NVARCHAR(50)
);

-- Tabela para armazenar informações dos discos de cada computador.
CREATE TABLE Discos (
    Id INT PRIMARY KEY IDENTITY,
    ComputadorMAC NVARCHAR(17) FOREIGN KEY REFERENCES Computadores(MAC) ON DELETE CASCADE,
    Letra NVARCHAR(10),
    TotalGB NVARCHAR(50),
    LivreGB NVARCHAR(50)
);

-- Tabela para armazenar informações das GPUs de cada computador.
-- Assumindo uma GPU por computador para simplicidade, mas poderia ser uma relação de muitos-para-muitos.
CREATE TABLE GPUs (
    Id INT PRIMARY KEY IDENTITY,
    ComputadorMAC NVARCHAR(17) FOREIGN KEY REFERENCES Computadores(MAC) ON DELETE CASCADE,
    Nome NVARCHAR(255),
    Fabricante NVARCHAR(100),
    RamDedicadaGB NVARCHAR(50)
);

-- Tabela para armazenar informações dos adaptadores de rede de cada computador.
CREATE TABLE AdaptadoresRede (
    Id INT PRIMARY KEY IDENTITY,
    ComputadorMAC NVARCHAR(17) FOREIGN KEY REFERENCES Computadores(MAC) ON DELETE CASCADE,
    Descricao NVARCHAR(255),
    EnderecoIP NVARCHAR(45),
    MascaraSubRede NVARCHAR(45),
    GatewayPadrao NVARCHAR(45),
    ServidoresDNS NVARCHAR(255)
);

-- Tabela para armazenar usuários do sistema web.
CREATE TABLE Usuarios (
    Id INT PRIMARY KEY IDENTITY,
    NomeUsuario NVARCHAR(100) UNIQUE NOT NULL,
    HashSenha NVARCHAR(255) NOT NULL,
    Role NVARCHAR(20) NOT NULL -- Ex: "Administrador", "Usuario"
);

-- Tabela para armazenar logs do sistema.
CREATE TABLE Logs (
    Id INT PRIMARY KEY IDENTITY,
    Timestamp DATETIME NOT NULL,
    Level NVARCHAR(10) NOT NULL,
    Message NVARCHAR(MAX) NOT NULL,
    Source NVARCHAR(50)
);
