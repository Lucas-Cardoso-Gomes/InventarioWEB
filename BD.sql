CREATE DATABASE Coletados;

use coletados;

CREATE TABLE Computadores (
            MAC varchar(100) PRIMARY KEY,
            IP varchar(100),
            Processador varchar(100),
            ProcessadorFabricante varchar(100),
            ProcessadorCore varchar(100),
            ProcessadorThread varchar(100),
            ProcessadorClock varchar(100),
            Ram varchar(100),
            RamTipo varchar(100),
            RamVelocidade varchar(100),
            RamVoltagem varchar(100),
            RamPorModule varchar(100),
            SO varchar(100),
            Usuario varchar(100),
            Hostname varchar(100),
            Fabricante varchar(100),
            ArmazenamentoC varchar(100),
            ArmazenamentoCTotal varchar(100),
            ArmazenamentoCLivre varchar(100),
            ArmazenamentoD varchar(100),
            ArmazenamentoDTotal varchar(100),
            ArmazenamentoDLivre varchar(100),
            ConsumoCPU varchar(100),
            DataColeta datetime
        );