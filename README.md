Inventário WEB e RMM (Remote Monitoring and Management)
O Inventário WEB é uma solução integrada de infraestrutura para a gestão de ativos de TI, monitorização de hardware e suporte remoto centralizado. A arquitetura é composta por um painel de administração Web e um Agente de Endpoint distribuído para as máquinas clientes.

Arquitetura do Sistema
1. Painel Web (Servidor)
Desenvolvido em ASP.NET Core MVC com .NET 8, funciona como o núcleo de governação e controlo.

Base de Dados: Utiliza SQLite gerido via Entity Framework Core, garantindo portabilidade e facilidade de implementação.

Comunicação em Tempo Real: Implementação de WebSockets através do SignalR para o chat de suporte e notificações instantâneas.

Segurança de Configuração: As credenciais e strings de conexão são protegidas através da leitura de ficheiros JSON embutidos como recursos do sistema (Embedded Resources).

Autenticação: Sistema baseado em Cookies com suporte para diferentes níveis de acesso (Admin, Coordenador, Colaborador, Diretoria/RH).

2. Agente de Coleta (Cliente)
Aplicação de consola desenvolvida em .NET 8, desenhada para execução em segundo plano nas estações de trabalho.

Servidor TCP Seguro: Escuta na porta 27275 utilizando comunicação encriptada SSL. O certificado é gerado dinamicamente em memória para evitar a persistência de chaves efémeras no disco.

Telemetria Avançada: Recolha de dados de hardware via WMI, incluindo estado do processador, memória RAM, armazenamento, fabricante e detalhes do sistema operativo.

Controlo Remoto: Capacidade de captura de ecrã (screenshot), simulação de eventos de rato e teclado, manipulação da área de transferência (clipboard) e execução de comandos de sistema.

Transferência de Ficheiros: Suporte para o carregamento de ficheiros diretamente para o ambiente de trabalho do utilizador remoto.

Funcionalidades Principais
Gestão de Ativos Diversificados: Registo detalhado de computadores, monitores, smartphones, periféricos e infraestrutura de rede.

Centro de Suporte (Help Desk): Sistema de tickets com histórico de conversas, anexos e priorização de chamados.

Monitorização Ativa: Serviço de background para verificação de disponibilidade (Ping) dos ativos de rede.

Auditoria e Manutenção: Registo histórico de intervenções técnicas (hardware e software) e logs persistentes de ações administrativas.

Guia de Compilação e Implementação
Publicação do Painel Web
Para gerar o executável do servidor sem dependências externas de runtime:

dotnet publish Web/Web.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

Publicação do Agente (Coleta)
O agente deve ser compilado e distribuído para as máquinas alvo:

dotnet publish Coleta/Coleta.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

Requisitos de Rede e Segurança
Firewall: É obrigatória a abertura da porta TCP 27275 nas máquinas clientes para permitir a receção de comandos do painel central.

Segurança de Endpoint: Devido às capacidades de controlo remoto (injeção de teclas e captura de ecrã), o executável do agente pode ser identificado como um falso positivo por soluções antivírus. Deve ser adicionado à lista de exclusões da política de segurança corporativa.

HTTPS: O servidor está configurado para suportar ambientes de produção, sendo recomendada a utilização de certificados válidos para a interface Web.

Estrutura de Dados
O sistema utiliza um esquema SQL organizado para garantir a integridade referencial entre colaboradores e os seus respetivos equipamentos, permitindo uma rastreabilidade total desde a entrega de um periférico até ao histórico de manutenção de um computador.