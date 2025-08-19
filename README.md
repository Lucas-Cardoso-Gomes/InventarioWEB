# Inventário Web e Agente de Coleta

## Descrição

Este projeto é um sistema de inventário de computadores baseado na web. Ele consiste em duas partes principais:

1.  **Aplicação Web (Web)**: Uma aplicação ASP.NET Core MVC que serve como painel de controle. Ela permite visualizar os computadores inventariados, seus detalhes de hardware e software, e enviar comandos remotos.
2.  **Agente de Coleta (Coleta)**: Uma aplicação de console .NET que roda nas máquinas clientes. Ela coleta informações detalhadas do sistema e responde a comandos enviados pela aplicação web.

## Funcionalidades

-   **Dashboard Web**: Visualize todos os computadores inventariados em um só lugar.
-   **Detalhes do Computador**: Veja informações detalhadas sobre cada máquina, incluindo hardware (CPU, RAM, armazenamento) e software (SO, usuário logado).
-   **Coleta de Dados Remota**: Inicie a coleta de dados para um IP específico ou uma faixa de IPs a partir do painel web.
-   **Envio de Comandos Remotos**: Envie comandos (como `ipconfig`, `gpupdate`) para um IP específico ou uma faixa de IPs.
-   **Logging de Eventos**: Uma página de Logs dedicada registra todas as ações importantes, como coletas de dados, comandos enviados e erros do sistema.
-   **Formulário de Criação Manual**: Adicione computadores manualmente ao sistema preenchendo todos os seus detalhes.

## Pré-requisitos

-   [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) ou superior.
-   [SQL Server](https://www.microsoft.com/sql-server/sql-server-downloads) (qualquer edição, incluindo Express).
-   Um editor de código como [Visual Studio](https://visualstudio.microsoft.com/) ou [VS Code](https://code.visualstudio.com/).

## Instalação e Configuração

### 1. Clonar o Repositório

```bash
git clone <URL_DO_REPOSITORIO>
cd <NOME_DO_DIRETORIO>
```

### 2. Configurar o Banco de Dados

1.  Certifique-se de que você tem uma instância do SQL Server em execução.
2.  Use um cliente de banco de dados (como SQL Server Management Studio ou Azure Data Studio) para criar um novo banco de dados (ex: `InventarioDB`).
3.  Execute o script `database_schema.sql` (localizado na raiz do projeto) neste banco de dados para criar as tabelas `Computadores` e `Logs`.

### 3. Configurar a Aplicação Web

1.  Abra o arquivo `Web/appsettings.json`.
2.  Encontre a seção `ConnectionStrings` e atualize a `DefaultConnection` com os detalhes da sua instância do SQL Server. Exemplo:

    ```json
    "ConnectionStrings": {
      "DefaultConnection": "Server=SEU_SERVIDOR;Database=InventarioDB;User Id=SEU_USUARIO;Password=SUA_SENHA;Trusted_Connection=False;"
    }
    ```
3. A aplicação Web também precisa das chaves de autenticação para se comunicar com o agente. Adicione a seguinte seção `Autenticacao` ao arquivo `Web/appsettings.json`, usando as mesmas senhas que você configurará no agente.

    ```json
    "Autenticacao": {
      "SolicitarInformacoes": "SUA_SENHA_SECRETA_PARA_COLETA",
      "RealizarComandos": "SUA_SENHA_SECRETA_PARA_COMANDOS"
    }
    ```

### 4. Configurar o Agente de Coleta

1.  Abra o arquivo `Coleta/appsettings.json`.
2.  Na seção `Autenticacao`, defina as senhas. Estas senhas devem ser as mesmas que você configurou na aplicação web.

    ```json
    "Autenticacao": {
      "SolicitarInformacoes": "SUA_SENHA_SECRETA_PARA_COLETA",
      "RealizarComandos": "SUA_SENHA_SECRETA_PARA_COMANDOS"
    }
    ```

## Como Executar

### 1. Executar o Agente de Coleta (Coleta)

1.  Navegue até o diretório `Coleta`: `cd Coleta`
2.  Execute o agente: `dotnet run`
3.  O agente de coleta deve ser executado em cada máquina que você deseja monitorar. Ele ficará escutando na porta 27275 por conexões da aplicação web.

### 2. Executar a Aplicação Web (Web)

1.  Navegue até o diretório `Web`: `cd Web`
2.  Execute a aplicação web: `dotnet run`
3.  Abra seu navegador e acesse a URL fornecida (geralmente `http://localhost:5000` ou `https://localhost:5001`).

## Estrutura do Projeto

-   `/Web`: Contém a aplicação web ASP.NET Core MVC.
    -   `/Controllers`: Lógica de controle para as páginas web.
    -   `/Views`: As páginas Razor (`.cshtml`) que compõem a UI.
    -   `/Models`: Os modelos de dados e view models.
    -   `/Services`: Lógica de negócio, como o `ColetaService` e `LogService`.
    -   `appsettings.json`: Arquivo de configuração da aplicação web.
-   `/Coleta`: Contém a aplicação de console .NET que atua como agente de coleta.
    -   `Program.cs`: O ponto de entrada do agente, onde o servidor TCP é iniciado.
    -   `appsettings.json`: Arquivo de configuração do agente.
-   `database_schema.sql`: Script SQL para criar a estrutura inicial do banco de dados.
