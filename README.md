# Inventário WEB & RMM (Remote Monitoring and Management)

O **Inventário WEB** é uma solução completa de infraestrutura desenhada para gerenciamento de ativos de TI, monitoramento de hardware e suporte remoto centralizado. A arquitetura é dividida em dois componentes principais: um painel de administração Web central e um Agente de Endpoint distribuído nas máquinas clientes.

## Arquitetura do Sistema

### 1. Painel Web (Servidor)
Desenvolvido em **ASP.NET Core MVC (.NET 8)**, atua como o painel central de governança.
* **Banco de Dados:** SQLite embutido via Entity Framework Core, facilitando a portabilidade.
* **Comunicação:** Utiliza SignalR para atualizações em tempo real (Chat de Suporte e Notificações).
* **Segurança:** Configurações sensíveis e strings de conexão são protegidas em memória via *Embedded Resources*.

### 2. Agente Coleta (Cliente)
Aplicação console em **.NET 8** projetada para rodar em segundo plano nas estações de trabalho dos colaboradores.
* **Servidor TCP:** Escuta ativamente na porta `27275` requisições autenticadas do painel central.
* **Telemetria:** Coleta dados em tempo real (Consumo de CPU, RAM, OS, MAC Address, Fabricante e Armazenamento via WMI).
* **Remote Control:** Permite a injeção remota de eventos de teclado, mouse, captura de tela, manipulação de clipboard e execução de processos invisíveis.

## Principais Funcionalidades

- **Gestão de Ativos:** Cadastro e acompanhamento de Computadores, Smartphones, Periféricos e Redes.
- **Help Desk Integrado:** Sistema de abertura de chamados (tickets) com chat em tempo real via WebSockets.
- **Suporte Remoto Oculto:** Visualização da tela do usuário e controle de interações diretamente pelo navegador, sem necessidade de softwares de terceiros (como TeamViewer/AnyDesk).
- **Log e Auditoria:** Rastreabilidade persistente de ações realizadas pelos administradores e relatórios exportáveis.
- **Deploy Facilitado:** Publicação em formato *Self-Contained* e *Single-File*, não exigindo a instalação do runtime do .NET nas estações alvo ou servidores legados.

## Guia de Implantação e Compilação

Como o sistema lida com dados confidenciais, a configuração exige compilação direta do código-fonte para embutir as credenciais de forma segura.

### Publicando o Painel Web
No terminal, dentro da pasta raiz da solução, execute:

dotnet publish Web/Web.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

O executável gerado não dependerá do framework instalado no servidor. É recomendável executar o serviço através do Agendador de Tarefas do Windows ou envelopado via NSSM.

### Publicando o Agente (Coleta)

dotnet publish Coleta/Coleta.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

Importante sobre o Agente: Para garantir a comunicação entre o Painel Web e os agentes nas estações de trabalho, é mandatório criar uma Regra de Entrada no Firewall do Windows liberando a porta TCP 27275 nas máquinas clientes (preferencialmente via GPO do Active Directory).

<<<<<<< Updated upstream
### Segurança e Governança
Este software possui capacidades de acesso remoto administrativo profundo (Envio de arquivos, Keystrokes, Screen Capture). Devido à sua natureza, sistemas de antivírus corporativos e o Windows Defender podem classificar o Agente (Coleta.exe) como um falso positivo. O executável deve ser incluído na lista de exclusão/confiança (Allowlist) da política de segurança de endpoints da sua infraestrutura.
=======
### 🛡️ Segurança e Governança
Este software possui capacidades de acesso remoto administrativo profundo (Envio de arquivos, Keystrokes, Screen Capture). Devido à sua natureza, sistemas de antivírus corporativos e o Windows Defender podem classificar o Agente (Coleta.exe) como um falso positivo. O executável deve ser incluído na lista de exclusão/confiança (Allowlist) da política de segurança de endpoints da sua infraestrutura.

### Geração de Certificado

Disponivel no repositório um gerador de certificado autoassinado, basta ajustar conforme dados do servidor que será instalado.
>>>>>>> Stashed changes
