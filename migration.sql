-- Adiciona a coluna para armazenar o CPF do colaborador associado ao usuário.
-- O CPF é a chave primária da tabela Colaboradores.
ALTER TABLE Usuarios
ADD ColaboradorCPF VARCHAR(14) NULL;

-- Adiciona a coluna para o ID do coordenador, que é uma referência à própria tabela de usuários.
-- Isso cria a relação de hierarquia entre coordenador e seus colaboradores.
ALTER TABLE Usuarios
ADD CoordenadorId INT NULL;

-- Adiciona a chave estrangeira para garantir a integridade referencial.
-- Um usuário (colaborador) agora pode ter um coordenador, que também é um usuário.
ALTER TABLE Usuarios
ADD CONSTRAINT FK_Usuarios_Coordenadores FOREIGN KEY (CoordenadorId) REFERENCES Usuarios(Id);
