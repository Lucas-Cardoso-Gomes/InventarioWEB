CREATE TABLE IF NOT EXISTS Colaboradores (
    CPF TEXT PRIMARY KEY,
    Nome TEXT NOT NULL UNIQUE,
    Email TEXT,
    SenhaEmail TEXT,
    Teams TEXT,
    SenhaTeams TEXT,
    EDespacho TEXT,
    SenhaEDespacho TEXT,
    Genius TEXT,
    SenhaGenius TEXT,
    Ibrooker TEXT,
    SenhaIbrooker TEXT,
    Adicional TEXT,
    SenhaAdicional TEXT,
    Filial TEXT,
    Setor TEXT,
    Smartphone TEXT,
    TelefoneFixo TEXT,
    Ramal TEXT,
    Alarme TEXT,
    Videoporteiro TEXT,
    Obs TEXT,
    DataInclusao TEXT NOT NULL,
    DataAlteracao TEXT,
    CoordenadorCPF TEXT,
    FOREIGN KEY (CoordenadorCPF) REFERENCES Colaboradores(CPF)
);

CREATE TABLE IF NOT EXISTS Usuarios (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Nome TEXT NOT NULL,
    Login TEXT NOT NULL UNIQUE,
    PasswordHash TEXT NOT NULL,
    Role TEXT NOT NULL CHECK (Role IN ('Admin', 'Coordenador', 'Colaborador', 'Diretoria/RH')),
    ColaboradorCPF TEXT,
    IsCoordinator INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (ColaboradorCPF) REFERENCES Colaboradores(CPF)
);

CREATE TABLE IF NOT EXISTS Computadores (
    MAC TEXT PRIMARY KEY,
    IP TEXT,
    ColaboradorCPF TEXT,
    Hostname TEXT NOT NULL,
    Fabricante TEXT,
    Processador TEXT,
    ProcessadorFabricante TEXT,
    ProcessadorCore TEXT,
    ProcessadorThread TEXT,
    ProcessadorClock TEXT,
    Ram TEXT,
    RamTipo TEXT,
    RamVelocidade TEXT,
    RamVoltagem TEXT,
    RamPorModule TEXT,
    ArmazenamentoC TEXT,
    ArmazenamentoCTotal TEXT,
    ArmazenamentoCLivre TEXT,
    ArmazenamentoD TEXT,
    ArmazenamentoDTotal TEXT,
    ArmazenamentoDLivre TEXT,
    ConsumoCPU TEXT,
    SO TEXT,
    DataColeta TEXT,
    PartNumber TEXT,
    FOREIGN KEY (ColaboradorCPF) REFERENCES Colaboradores(CPF)
);

CREATE TABLE IF NOT EXISTS Monitores (
    PartNumber TEXT PRIMARY KEY,
    ColaboradorCPF TEXT,
    Marca TEXT,
    Modelo TEXT NOT NULL,
    Tamanho TEXT NOT NULL,
    FOREIGN KEY (ColaboradorCPF) REFERENCES Colaboradores(CPF)
);

CREATE TABLE IF NOT EXISTS Perifericos (
    PartNumber TEXT PRIMARY KEY,
    ColaboradorCPF TEXT,
    Tipo TEXT NOT NULL,
    DataEntrega TEXT,
    FOREIGN KEY (ColaboradorCPF) REFERENCES Colaboradores(CPF)
);

CREATE TABLE IF NOT EXISTS Manutencoes (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ComputadorMAC TEXT,
    MonitorPartNumber TEXT,
    PerifericoPartNumber TEXT,
    DataManutencaoHardware TEXT,
    DataManutencaoSoftware TEXT,
    ManutencaoExterna TEXT,
    Data TEXT,
    Historico TEXT,
    FOREIGN KEY (ComputadorMAC) REFERENCES Computadores(MAC),
    FOREIGN KEY (MonitorPartNumber) REFERENCES Monitores(PartNumber),
    FOREIGN KEY (PerifericoPartNumber) REFERENCES Perifericos(PartNumber)
);

CREATE TABLE IF NOT EXISTS Chamados (
    ID INTEGER PRIMARY KEY AUTOINCREMENT,
    AdminCPF TEXT,
    ColaboradorCPF TEXT NOT NULL,
    Servico TEXT NOT NULL,
    Descricao TEXT NOT NULL,
    DataAlteracao TEXT,
    DataCriacao TEXT NOT NULL,
    Status TEXT NOT NULL DEFAULT 'Aberto' CHECK (Status IN ('Aberto', 'Em Andamento', 'Fechado')),
    Prioridade TEXT NOT NULL DEFAULT 'Médio' CHECK (Prioridade IN ('Crítico', 'Alto', 'Médio', 'Baixo')),
    FOREIGN KEY (AdminCPF) REFERENCES Colaboradores(CPF),
    FOREIGN KEY (ColaboradorCPF) REFERENCES Colaboradores(CPF)
);

CREATE TABLE IF NOT EXISTS ChamadoConversas (
    ID INTEGER PRIMARY KEY AUTOINCREMENT,
    ChamadoID INTEGER NOT NULL,
    UsuarioCPF TEXT NOT NULL,
    Mensagem TEXT NOT NULL,
    DataCriacao TEXT NOT NULL,
    FOREIGN KEY (ChamadoID) REFERENCES Chamados(ID) ON DELETE CASCADE,
    FOREIGN KEY (UsuarioCPF) REFERENCES Colaboradores(CPF)
);

CREATE TABLE IF NOT EXISTS ChamadoAnexos (
    ID INTEGER PRIMARY KEY AUTOINCREMENT,
    ChamadoID INTEGER NOT NULL,
    NomeArquivo TEXT NOT NULL,
    CaminhoArquivo TEXT NOT NULL,
    DataUpload TEXT NOT NULL,
    FOREIGN KEY (ChamadoID) REFERENCES Chamados(ID) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS Rede (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Tipo TEXT NOT NULL,
    IP TEXT NOT NULL,
    MAC TEXT,
    Nome TEXT NOT NULL,
    DataInclusao TEXT NOT NULL,
    DataAlteracao TEXT,
    Observacao TEXT
);

CREATE TABLE IF NOT EXISTS Smartphones (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Modelo TEXT NOT NULL,
    IMEI1 TEXT NOT NULL,
    IMEI2 TEXT,
    Usuario TEXT,
    Filial TEXT,
    DataCriacao TEXT NOT NULL,
    DataAlteracao TEXT,
    ContaGoogle TEXT,
    SenhaGoogle TEXT,
    MAC TEXT
);

INSERT INTO Usuarios (Nome, Login, PasswordHash, Role, IsCoordinator)
SELECT 'Admin', 'Admin', 'Admin', 'Admin', 0
WHERE NOT EXISTS (SELECT 1 FROM Usuarios WHERE Login = 'Admin');
