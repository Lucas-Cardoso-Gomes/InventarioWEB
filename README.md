# Inventário WEB e RMM (Remote Monitoring and Management)

O **Inventário WEB e RMM** é uma solução integrada para gestão de ativos de TI, monitoramento em tempo real de hardware/infraestrutura e suporte remoto centralizado. A arquitetura é composta por um painel de administração Web e um agente de endpoint distribuído (`Coleta`) executado nas máquinas monitoradas.

---

## 🏗 Arquitetura do Sistema

### 1. Painel Web (Servidor Central)
Desenvolvido em **ASP.NET Core MVC** (.NET 8.0) no formato de monolito modular e responsivo.

* **Banco de Dados Relacional:** SQLite embutido, acessado via **ADO.NET Puro** (`Microsoft.Data.Sqlite`) com controle direto de consultas SQL e atualização automatizada do esquema (`DatabaseService.cs`).
* **Comunicação em Tempo Real:** WebSockets através do **SignalR** para suporte interativo via Chat (`ChatHub`) e notificações instantâneas do sistema (`NotificationHub`).
* **Autenticação e Sessões:** Sistema baseado em Cookies (`CookieAuthenticationDefaults`) com validação ativa contínua (`OnValidatePrincipal`) para invalidação imediata de sessões inativas ou removidas.
* **Controle de Acesso em Níveis (RBAC):** Permissões granulares configuradas para perfis:
  * `Admin`: Acesso irrestrito a configurações, usuários e ações do sistema.
  * `Diretoria/RH`: Visualização e gestão estratégica.
  * `Coordenador`: Gestão dos colaboradores e ativos sob sua coordenação.
  * `Colaborador`: Acesso restrito aos próprios ativos e chamados de suporte.
* **Comunicação por E-mail:** Integração via **MailKit / MimeKit** suportando autenticação SSL/TLS para notificações e chamados.
* **Proxy Reverso:** Suporte nativo ao middleware de `ForwardedHeaders` para operação transparente atrás de proxies reversos como Nginx, IIS ou Apache.

### 2. Agente de Coleta (Endpoint Agent)
Aplicação de console e serviço em segundo plano desenvolvida em **.NET 8.0**, focada em alta performance e baixo consumo de recursos nas estações de trabalho Windows.

* **Servidor TCP SSL com Certificate Pinning:** Escuta na porta TCP `27275` utilizando TLS/SSL. O certificado digital é gerado dinamicamente em memória com chave RSA estática importada do arquivo de configuração, permitindo a autenticação segura do agente e prevenindo ataques *Man-in-the-Middle* (MitM).
* **Autenticação HMAC:** Desafio com Nonce gerado dinamicamente no aperto de mão (handshake) TCP e verificação HMAC-SHA256 de tokens de autorização.
* **Telemetria de Hardware (Push Inicial & Polling):**
  * Coleta automática e envio inicial de telemetria para o servidor Web no startup (`/api/agent/telemetry`).
  * Consultas locais via WMI (`Win32_Processor`, `Win32_OperatingSystem`, `Win32_DiskDrive`, `Win32_PhysicalMemory`, `MSAcpi_ThermalZoneTemperature`, etc.).
  * Coleta avançada de softwares instalados via Registro do Windows (HKLM/HKCU) combinada com parsing de saída do `winget`.
* **Acesso e Suporte Remoto Interativo:**
  * **Captura de Tela Continuada:** Transmissão de screenshots e stream de vídeo contínuo.
  * **Injeção de Mouse e Teclado:** Controle de cursor (normalização para múltiplos monitores) e digitação em tempo real.
  * **Atalhos do Sistema e Clipboard:** Suporte ao envio de atalhos (ex: Task Manager / Ctrl+Alt+Del) e sincronização bidirecional do clipboard.
  * **Upload de Arquivos:** Envio seguro e direto de arquivos para a Área de Trabalho do usuário remoto.
  * **Execução Remota de Comandos:** Execução segura de comandos PowerShell/CMD na estação do cliente.

---

## 🚀 Funcionalidades Principais

* 💻 **Gestão Completa de Computadores:** Leitura e detalhamento de CPU, RAM, discos e partições, temperatura do processador, desgaste da bateria (Wear Level), tempo de atividade (uptime) e histórico de uso de CPU.
* 📱 **Ativos de Infraestrutura e Dispositivos Móveis:**
  * **Monitores & Periféricos:** Controle detalhado e associação com colaboradores e máquinas.
  * **Smartphones:** Registro com suporte a múltiplos IMEIs em tabela normalizada.
  * **Redes & Impressoras:** Cadastro de IPs, MACs, tipo de dispositivo e equipamentos de rede.
* 👨‍💼 **Gestão de Colaboradores e Credenciais:** Cadastro unificado por CPF, associando cargos, telefones, equipamentos em uso e credenciais/acessos atribuídos.
* 🛠 **Manutenções e Histórico de Trocas:** Registro e auditoria completa de intervenções técnicas (hardware/software) e substituição de peças/máquinas.
* 🔄 **Monitoramento de Disponibilidade (Ping):** Serviço de segundo plano (`PingService`) executando verificações contínuas de pacotes para ativos na rede.
* 🎫 **Central de Suporte (Help Desk):**
  * Criação, priorização e acompanhamento de chamados técnicos.
  * Chat em tempo real entre usuário e equipe de suporte.
  * Anexo de arquivos e alteração de status.
* 📊 **Exportação de Dados:** Exportação completa de relatórios operacionais e inventário em planilhas Excel (`.xlsx`) via biblioteca **EPPlus**.

---

## 🛠 Guia de Instalação e Execução

### Pré-requisitos
* [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) instalado.

### Configuração Inicial

1. **Configuração do Servidor Web (`Web/`):**
   * Copie o arquivo de exemplo `Web/appsettings copy.json` para `Web/appsettings.json`:
     ```bash
     cp Web/"appsettings copy.json" Web/appsettings.json
     ```
   * Caso queira testar em ambiente local/desenvolvimento sem erros de certificado Kestrel no modo Production, você também pode sobrescrever ou copiar para `Web/appsettings.Production.json`.
   * **Credencial Padrão de Administrador:**
     * **Usuário:** `Admin`
     * **Senha:** `Admin`

2. **Configuração do Agente (`Coleta/`):**
   * Copie o arquivo de exemplo `Coleta/appsettings copy.json` para `Coleta/appsettings.json`:
     ```bash
     cp Coleta/"appsettings copy.json" Coleta/appsettings.json
     ```

### Executando em Modo de Desenvolvimento

* **Iniciando o Servidor Web:**
  ```bash
  dotnet run --project Web/Web.csproj --urls "http://localhost:5000"
  ```
  Acesse a aplicação no navegador em `http://localhost:5000`.

* **Iniciando o Agente Coleta:**
  ```bash
  dotnet run --project Coleta/Coleta.csproj
  ```

---

## 📦 Compilação e Publicação para Produção

* **Publicar Painel Web (Self-Contained Windows x64):**
  ```bash
  dotnet publish Web/Web.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
  ```

* **Publicar Agente Coleta (Self-Contained Windows x64):**
  ```bash
  dotnet publish Coleta/Coleta.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
  ```

---

## 🔒 Requisitos de Rede e Segurança

1. **Porta do Agente (TCP 27275):** O agente escuta requisições na porta `27275`. Regras de firewall no cliente ou rede corporativa devem autorizar o tráfego TCP nessa porta.
2. **Políticas de Antivírus / Endpoint:** Em virtude do agente possuir capacidades de suporte remoto (injeção de eventos de mouse/teclado, streaming de tela e execução de comandos), certifique-se de adicionar o executável às exceções do antivírus corporativo.
3. **Comunicação HTTPS / WSS:** Em produção, recomenda-se expor o Painel Web sob protocolo HTTPS seguro com suporte a WebSockets habilitado no Proxy Reverso (Nginx / IIS / Caddy).
