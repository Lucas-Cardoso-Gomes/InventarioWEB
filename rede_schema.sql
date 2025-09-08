CREATE TABLE Rede (
    Id INT PRIMARY KEY IDENTITY,
    Tipo NVARCHAR(50) NOT NULL,
    IP NVARCHAR(45) NOT NULL,
    MAC NVARCHAR(17),
    Nome NVARCHAR(100) NOT NULL,
    DataInclusao DATETIME NOT NULL,
    DataAlteracao DATETIME,
    Observacao NVARCHAR(MAX),
    Status NVARCHAR(10),
    LastPingStatus BIT,
    PreviousPingStatus BIT
);
