# 🌐 Painel Web Central (Web Project)

O **Painel Web Central** é a aplicação web de gerenciamento e monitoramento desenvolvida em **ASP.NET Core MVC (.NET 8.0)**. Ele centraliza o **inventário de ativos de TI**, o **suporte e controle remoto interativo (RMM)**, o **Help Desk**, o **controle de acesso (RBAC)** e a **gestão de colaboradores**.

---

## 🏗 Arquitetura do Sistema

### 1. ASP.NET Core MVC (.NET 8.0)
* **Arquitetura Monolítica Modular:** Desenvolvido sem dependências de frameworks pesados de ORM, priorizando alta performance, baixo consumo de memória e total previsibilidade das consultas.
* **Acesso a Dados via ADO.NET Puro:** Utiliza o driver `Microsoft.Data.Sqlite` para executar SQL queries e manipulação direta de dados SQLite (`Web/Coletados.db` e `Web/Logs.db`).
* **WebSockets em Tempo Real via SignalR:** Gerencia comunicação bidirecional para suporte por chat (`ChatHub`) e entrega instantânea de notificações no navegador (`NotificationHub`).

### 2. Autenticação, Sessão e Segurança (RBAC)
* **Autenticação por Cookie (`CookieAuthenticationDefaults`):** Configurado com verificação contínua do estado do usuário no banco de dados através do evento `OnValidatePrincipal`. Se um usuário for desativado ou excluído, sua sessão é revogada imediatamente no próximo clique/requisição.
* **Controle de Acesso Baseado em Perfis (RBAC):**
  * `Admin`: Acesso total e ilimitado a todas as telas, configurações e ações do sistema.
  * `Diretoria/RH`: Acesso a relatórios de colaboradores, ativos, inventários e movimentações.
  * `Coordenador`: Acesso restrito para visualizar e gerenciar colaboradores e ativos subordinados ao seu setor/área.
  * `Colaborador`: Permissão restrita para visualizar seus próprios equipamentos atribuídos e abrir/acompanhar chamados de suporte.

---

## 📂 Estrutura de Direitórios e Módulos

### 1. Controllers (`Web/Controllers/`)

| Controller | Função e Responsabilidade |
| :--- | :--- |
| **`AccountController.cs`** | Gerencia autenticação de usuários: Login, Logout, Alteração de Senha e validação de claims. |
| **`ChamadosController.cs`** | Gestão do Help Desk: Abertura, atribuição, alteração de status, priorização, anexo de arquivos e histórico do chamado. |
| **`ColaboradoresController.cs`** | Cadastro completo de colaboradores (identificados por CPF unificado), associação de cargos, coordenação, telefones e credenciais de acesso. |
| **`ComputadoresController.cs`** | Gestão de inventário de estações e servidores: detalhes de CPU, RAM, discos, partições, temperatura, Wear Level de bateria e tempo de atividade. |
| **`DashboardController.cs`** | Painel principal com estatísticas executivas: contagem de ativos, alertas de temperatura/disco, chamados abertos e status de disponibilidade da rede. |
| **`DbMigrationController.cs` / `MigrationController.cs`** | Utilitários de migração e manutenção do banco de dados SQLite. |
| **`ExportarController.cs`** | Geração e download de relatórios operacionais e inventário consolidado em formato Excel (`.xlsx`) utilizando **EPPlus**. |
| **`FeedbacksController.cs`** | Coleta e visualização de avaliações e pesquisas de satisfação dos usuários após a conclusão dos chamados. |
| **`GerenciamentoController.cs`** | Central unificada de inventário de infraestrutura e atribuição de equipamentos. |
| **`ManutencoesController.cs`** | Registro e auditoria de manutenções preventivas e corretivas executadas nos equipamentos. |
| **`MonitoramentoController.cs`** | Visualização do status de disponibilidade em tempo real e gráficos de ping de ativos da rede. |
| **`MonitoresController.cs`** | Cadastro, especificação e associação de monitores de vídeo. |
| **`PerifericosController.cs`** | Cadastro e controle de periféricos (teclados, mouses, nobreaks, headsets, etc.). |
| **`ProgramasController.cs`** | Visualização do inventário consolidado de softwares instalados nas estações, obtidos via agente. |
| **`RedesController.cs`** | Mapeamento de equipamentos de rede, roteadores, switches, impressoras e alocação de IPs/MACs. |
| **`RemoteAccessController.cs`** | Interface de suporte remoto (RMM): conexão TCP SSL segura com o agente `Coleta`, streaming de tela, injeção de periféricos e transferência de arquivos. |
| **`ScreenCaptureController.cs`** | API e utilitário para recepção e exibição de capturas de tela instantâneas. |
| **`SmartphonesController.cs`** | Registro e gestão de frota móvel (smartphones e tablets) com suporte a múltiplos IMEIs. |
| **`UsersController.cs`** | Gerenciamento de usuários do sistema, criação de logins e atribuição de papéis (RBAC). |
| **`Api/AgentController.cs`** | Endpoint REST `/api/agent/telemetry` que recebe e processa o push inicial de telemetria enviado ativamente pelo agente de coleta. |

---

### 2. Serviços da Aplicação (`Web/Services/`)

| Serviço | Função e Responsabilidade |
| :--- | :--- |
| **`DatabaseService.cs`** | Gerencia a conexão com o SQLite, inicialização automática do banco e execução de scripts de alteração de tabela (`ApplySchemaUpdates`). |
| **`ColetaService.cs`** | Comunica com o agente `Coleta` via TCP SSL (porta 27275), executando o handshake HMAC Nonce e persistindo a telemetria recebida no banco de dados. |
| **`ComandoService.cs`** | Envia comandos remotos, scripts e solicitações ao agente de coleta. |
| **`UserService.cs`** | Regras de negócio de usuários, criação, hash de senha e consulta de permissões. |
| **`ManutencaoService.cs`** | Lógica de histórico de intervenções técnicas em equipamentos. |
| **`HistoricoTrocasService.cs`** | Registro e rastreabilidade de trocas de peças, substituições de equipamentos e atribuições entre colaboradores. |
| **`SmartphoneService.cs`** | Regras de negócio para cadastro de smartphones e relacionamento normalizado de IMEIs. |
| **`PingService.cs`** | Serviço executado em segundo plano (`BackgroundService`) que realiza pings ICMP periódicos nos ativos de rede para monitorar disponibilidade. |
| **`EmailService.cs` / `IEmailService.cs`** | Envio de e-mails de notificação do Help Desk e alertas utilizando **MailKit / MimeKit** (suporte a SSL/TLS implícito). |
| **`LogService.cs` / `PersistentLogService.cs`** | Registro auditável de logs do sistema em banco de dados SQLite separado (`Web/Logs.db`). |
| **`DataMigrationService.cs`** | Auxiliar no processo de atualização e conversão de dados do banco de dados. |

---

### 3. Comunicação em Tempo Real (`Web/Hubs/`)

* **`ChatHub.cs`:** Hub SignalR que gerencia as salas de bate-papo em tempo real nos chamados de suporte entre os usuários solicitantes e a equipe de TI.
* **`NotificationHub.cs`:** Hub SignalR global responsável por emitir notificações push instantâneas no navegador para atualizações de chamados e alertas críticos de sistema.

---

## 🗄️ Banco de Dados e Esquema Relacional

O sistema utiliza banco de dados **SQLite** embutido (`Web/Coletados.db` para dados do sistema e `Web/Logs.db` para auditoria de logs). O esquema atende às normas de **Primeira Forma Normal (1NF)** através do uso de tabelas filhas normalizadas com relacionamentos de chave estrangeira (`ON DELETE CASCADE`):

* **`schema_main.sql`:** Contém o DDL para criação das tabelas principais:
  * `Usuarios`, `Colaboradores`, `ColaboradorTelefones`, `ColaboradorCredenciais`.
  * `Computadores`, `Processadores`, `ComputadorDiscos`, `HistoricoCPU`.
  * `Smartphones`, `SmartphoneIMEIs`.
  * `Monitores`, `Perifericos`, `EquipamentosRede`.
  * `Chamados`, `ChamadoAnexos`, `ChamadoHistorico`, `Feedbacks`.
  * `Manutencoes`, `HistoricoTrocas`.
* **`schema_logs.sql`:** DDL da tabela auditável `SystemLogs`.

---

## 🛠⚙️ Configuração do Servidor

O comportamento do servidor Web é configurado através dos arquivos `appsettings.json` ou `appsettings.Production.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=Coletados.db",
    "LogsConnection": "Data Source=Logs.db"
  },
  "Smtp": {
    "Server": "smtp.exemplo.com",
    "Port": 587,
    "UseSsl": true,
    "User": "notificacoes@exemplo.com",
    "Password": "SuaSenhaSmtpSegura"
  }
}
```

---

## 🚀 Execução e Publicação

### 1. Certificado SSL em Produção
O repositório inclui o script **`Geração de certificado copy.bat`** que utiliza a ferramenta `openssl` ou `powershell` para gerar certificados digitais para uso no Kestrel/IIS em ambientes de produção.

### 2. Executando em Desenvolvimento
```bash
dotnet run --project Web/Web.csproj --urls "http://localhost:5000"
```
* **Acesso Inicial:** `http://localhost:5000`
* **Usuário Padrão:** `Admin`
* **Senha Padrão:** `Admin`

### 3. Publicação para Produção (Windows x64 Single File)
```bash
dotnet publish Web/Web.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```
Os arquivos compilados serão gerados na pasta `Web/bin/Release/net8.0/win-x64/publish/`.
