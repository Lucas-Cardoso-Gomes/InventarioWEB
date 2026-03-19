#Projeto InventárioWEB
O Projeto InventárioWEB é um sistema completo para o monitoramento e gerenciamento de ativos de TI. Ele consiste em dois componentes principais: um agente de coleta de dados (Coleta) e uma aplicação web (Web) para visualização e gerenciamento.

Arquitetura
O sistema segue uma arquitetura cliente-servidor:

Coleta (Agente): Uma aplicação de console .NET que roda nas máquinas dos clientes. Ele coleta informações de hardware e software e aguarda por comandos através de um socket TCP.

Web (Painel de Controle): Uma aplicação ASP.NET Core MVC que fornece um painel centralizado. Ela se comunica com os agentes para obter dados e enviar comandos. Também utiliza um banco de dados central para armazenar dados históricos, informações de ativos, dados de usuários e tickets de suporte.

Banco de Dados: O backend utiliza um banco de dados Microsoft SQL Server para persistir todas as informações. O esquema completo do banco de dados, incluindo tabelas, triggers e relacionamentos, pode ser encontrado no arquivo schema_completo.sql.

Funcionalidades
Coleta de Dados em Tempo Real: Reúne informações do sistema como CPU, RAM, Sistema Operacional, fabricante e uso de disco.

Execução Remota de Comandos: Permite que administradores executem comandos de shell nas máquinas dos clientes a partir da interface web.

Gerenciamento de Ativos: Mantém o controle de computadores, monitores, periféricos e suas alocações aos funcionários (Colaboradores).

Gerenciamento de Usuários: Controle de acesso baseado em funções para diferentes tipos de usuários (Admin, Coordenador, etc.).

Sistema de Tickets: Um sistema de help desk integrado para gerenciar solicitações de suporte, completo com chat em tempo real (via SignalR) e anexos de arquivos.

Registro de Atividades (Logging): Registro abrangente de ações (Inserções, Atualizações, Exclusões) em tabelas críticas para fins de auditoria.

Notificações: Notificações em tempo real para eventos do sistema usando SignalR e e-mail.

Pré-requisitos
.NET 8.0 SDK

Microsoft SQL Server

Configuração e Instalação
1. Configuração do Banco de Dados
Garanta que você tenha uma instância do Microsoft SQL Server em execução.

Execute o script schema_completo.sql em sua instância de banco de dados. Isso criará o banco de dados Coletados, todas as tabelas necessárias e os triggers de auditoria.

Anote sua string de conexão.

2. Configuração do Agente Coleta
O agente está localizado no diretório Coleta/.

Configuração:

Navegue até o diretório Coleta/.

Crie um arquivo appsettings.json. Você pode usar appsettings copy.json como modelo.

Configure as chaves de Autenticacao. Estes são segredos compartilhados usados pela aplicação Web para autenticar suas requisições.

JSON

{
  "Autenticacao": {
    "SolicitarInformacoes": "SuaChaveSecretaParaInfo",
    "RealizarComandos": "SuaChaveSecretaParaComandos"
  }
}
Executando o Agente:

Abra um terminal no diretório Coleta/.

Execute o comando: dotnet run

O agente começará a escutar na porta 27275. Certifique-se de que esta porta esteja aberta no firewall da máquina cliente.

3. Configuração da Aplicação Web
A aplicação web está localizada no diretório Web/.

Configuração:

Navegue até o diretório Web/.

A aplicação usa o .NET Secret Manager para configurações sensíveis. Você precisará configurar as seguintes definições.

Conexão com o Banco de Dados:

Bash

dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Sua_String_De_Conexao_SQL_Server"
Autenticação do Agente: Defina as mesmas chaves secretas que você configurou no appsettings.json do agente.

Bash

dotnet user-secrets set "AgentAuth:InfoSecret" "SuaChaveSecretaParaInfo"
dotnet user-secrets set "AgentAuth:CommandSecret" "SuaChaveSecretaParaComandos"
Configurações de E-mail (Opcional): Para que as notificações por e-mail funcionem, configure as definições do seu servidor SMTP.

Bash

dotnet user-secrets set "EmailSettings:SmtpServer" "smtp.example.com"
dotnet user-secrets set "EmailSettings:Port" "587"
dotnet user-secrets set "EmailSettings:SenderName" "TI Vise"
dotnet user-secrets set "EmailSettings:SenderEmail" "noreply@example.com"
dotnet user-secrets set "EmailSettings:Username" "seu-usuario-email"
dotnet user-secrets set "EmailSettings:Password" "sua-senha-email"
Executando a Aplicação Web:

Abra um terminal no diretório raiz do projeto.

Execute o comando: dotnet run --project Web

A aplicação estará acessível em http://localhost:80 e https://localhost:443 (ou nas portas configuradas em Properties/launchSettings.json).

Como Funciona
Fluxo de Dados
O agente Coleta é executado na máquina de um usuário, aguardando passivamente por conexões.

Um Administrador, usando a aplicação Web, navega para a página de detalhes de um computador.

A aplicação Web abre uma conexão TCP com o endereço IP e a porta do agente.

Ela envia o segredo de autenticação apropriado (SolicitarInformacoes).

O agente valida o segredo e envia de volta um objeto JSON contendo as informações mais recentes do sistema.

A aplicação Web recebe o JSON, atualiza seu banco de dados e exibe as informações para o Administrador.

Comandos Remotos
O fluxo para comandos remotos é semelhante, mas a aplicação Web envia o segredo RealizarComandos, seguido pelo comando a ser executado. O agente retorna a saída do comando.