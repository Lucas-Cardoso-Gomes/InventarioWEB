-- Este script de migração atualiza o esquema do banco de dados para corresponder aos modelos C# atualizados.
-- Aplique este script a um banco de dados criado com a versão original de schema_completo.sql.

-- Adiciona a restrição NOT NULL à coluna Hostname na tabela Computadores.
-- Assumindo que não há registros existentes com Hostname nulo. Se houver, eles precisam ser corrigidos primeiro.
ALTER TABLE Computadores
ALTER COLUMN Hostname NVARCHAR(100) NOT NULL;

-- Adiciona a restrição NOT NULL à coluna Modelo na tabela Monitores.
-- Assumindo que não há registros existentes com Modelo nulo.
ALTER TABLE Monitores
ALTER COLUMN Modelo NVARCHAR(50) NOT NULL;

-- Adiciona a restrição NOT NULL à coluna Tamanho na tabela Monitores.
-- Assumindo que não há registros existentes com Tamanho nulo.
ALTER TABLE Monitores
ALTER COLUMN Tamanho NVARCHAR(20) NOT NULL;

-- Adiciona a restrição NOT NULL à coluna Tipo na tabela Perifericos.
-- Assumindo que não há registros existentes com Tipo nulo.
ALTER TABLE Perifericos
ALTER COLUMN Tipo NVARCHAR(50) NOT NULL;
