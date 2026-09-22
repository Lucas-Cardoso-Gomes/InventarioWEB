CREATE TABLE IF NOT EXISTS Colaboradores (
    CPF TEXT PRIMARY KEY,
    Nome TEXT NOT NULL UNIQUE,
    Email TEXT,
    Filial TEXT,
    Setor TEXT,
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
    IsActive INTEGER NOT NULL DEFAULT 1,
    FOREIGN KEY (ColaboradorCPF) REFERENCES Colaboradores(CPF)
);

CREATE TABLE IF NOT EXISTS Feedbacks (
    ID INTEGER PRIMARY KEY AUTOINCREMENT,
    Protocolo TEXT NOT NULL UNIQUE,
    Assunto TEXT NOT NULL,
    Mensagem TEXT NOT NULL,
    DataCriacao TEXT NOT NULL,
    UsuarioCPF TEXT,
    Status TEXT NOT NULL DEFAULT 'Aberto' CHECK (Status IN ('Aberto', 'Em Andamento', 'Fechado')),
    FOREIGN KEY (UsuarioCPF) REFERENCES Colaboradores(CPF)
);

CREATE TABLE IF NOT EXISTS FeedbackConversas (
    ID INTEGER PRIMARY KEY AUTOINCREMENT,
    FeedbackID INTEGER NOT NULL,
    UsuarioCPF TEXT, -- Pode ser null se for o autor anônimo ou admin não vinculado
    Remetente TEXT NOT NULL, -- "Anônimo", "Admin", ou Nome
    Mensagem TEXT NOT NULL,
    DataCriacao TEXT NOT NULL,
    FOREIGN KEY (FeedbackID) REFERENCES Feedbacks(ID) ON DELETE CASCADE,
    FOREIGN KEY (UsuarioCPF) REFERENCES Colaboradores(CPF)
);

CREATE TABLE IF NOT EXISTS ProgramasInstalados (
    ID INTEGER PRIMARY KEY AUTOINCREMENT,
    ComputadorMAC TEXT NOT NULL,
    Nome TEXT NOT NULL,
    Versao TEXT,
    Desenvolvedor TEXT,
    PacoteId TEXT,
    DataColeta TEXT NOT NULL,
    FOREIGN KEY (ComputadorMAC) REFERENCES Computadores(MAC) ON DELETE CASCADE
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
    ProcessadorTemperatura TEXT,
    Ram TEXT,
    RamTipo TEXT,
    RamVelocidade TEXT,
    RamVoltagem TEXT,
    RamPorModule TEXT,
    ConsumoCPU TEXT,
    SO TEXT,
    DataColeta TEXT,
    PartNumber TEXT,
    DataGarantia TEXT,
    Backup TEXT,
    BateriaWearLevel TEXT,
    TempoAtividade TEXT,
    Localizacao TEXT,
    FOREIGN KEY (ColaboradorCPF) REFERENCES Colaboradores(CPF)
);

CREATE TABLE IF NOT EXISTS Monitores (
    PartNumber TEXT PRIMARY KEY,
    ColaboradorCPF TEXT,
    Marca TEXT,
    Modelo TEXT NOT NULL,
    Tamanho TEXT NOT NULL,
    DataGarantia TEXT,
    FOREIGN KEY (ColaboradorCPF) REFERENCES Colaboradores(CPF)
);

CREATE TABLE IF NOT EXISTS Perifericos (
    PartNumber TEXT PRIMARY KEY,
    ColaboradorCPF TEXT,
    Tipo TEXT NOT NULL,
    DataEntrega TEXT,
    DataGarantia TEXT,
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
    Lido INTEGER NOT NULL DEFAULT 0,
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
    Observacao TEXT,
    Localizacao TEXT,
    Endereco TEXT,
    DataGarantia TEXT
);

CREATE TABLE IF NOT EXISTS Smartphones (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Modelo TEXT NOT NULL,
    Usuario TEXT,
    Filial TEXT,
    DataCriacao TEXT NOT NULL,
    DataAlteracao TEXT,
    ContaGoogle TEXT,
    SenhaGoogle TEXT,
    MAC TEXT,
    DataGarantia TEXT
);

CREATE TABLE IF NOT EXISTS SmartphoneIMEIs (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    SmartphoneId INTEGER NOT NULL,
    IMEI TEXT NOT NULL,
    Ordem INTEGER NOT NULL DEFAULT 1,
    FOREIGN KEY (SmartphoneId) REFERENCES Smartphones(Id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS ComputadorDiscos (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ComputadorMAC TEXT NOT NULL,
    Letra TEXT NOT NULL,
    TotalGB TEXT,
    LivreGB TEXT,
    FOREIGN KEY (ComputadorMAC) REFERENCES Computadores(MAC) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS ColaboradorCredenciais (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ColaboradorCPF TEXT NOT NULL,
    Sistema TEXT NOT NULL,
    UsuarioSistema TEXT,
    SenhaSistema TEXT,
    FOREIGN KEY (ColaboradorCPF) REFERENCES Colaboradores(CPF) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS ColaboradorTelefones (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ColaboradorCPF TEXT NOT NULL,
    Tipo TEXT NOT NULL,
    Numero TEXT NOT NULL,
    FOREIGN KEY (ColaboradorCPF) REFERENCES Colaboradores(CPF) ON DELETE CASCADE
);

INSERT INTO Usuarios (Nome, Login, PasswordHash, Role, IsCoordinator)
SELECT 'Admin', 'Admin', 'Admin', 'Admin', 0
WHERE NOT EXISTS (SELECT 1 FROM Usuarios WHERE Login = 'Admin');
CREATE TABLE IF NOT EXISTS HistoricoTrocas (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    EquipamentoId TEXT NOT NULL,
    TipoEquipamento TEXT NOT NULL,
    CampoAlterado TEXT NOT NULL,
    ValorAntigo TEXT,
    ValorNovo TEXT,
    DataAlteracao TEXT NOT NULL,
    Usuario TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS HistoricoCPU (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ComputadorMAC TEXT NOT NULL,
    Consumo REAL NOT NULL,
    DataColeta TEXT NOT NULL,
    FOREIGN KEY (ComputadorMAC) REFERENCES Computadores(MAC) ON DELETE CASCADE
);
