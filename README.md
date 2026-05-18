# Inventário WEB e RMM (Remote Monitoring and Management)

O Inventário WEB é uma solução integrada de infraestrutura para a gestão de ativos de TI, monitoramento de hardware e suporte remoto centralizado. A arquitetura é composta por um painel de administração Web e um Agente de Endpoint distribuído para as máquinas clientes.

## Arquitetura do Sistema

### 1. Painel Web (Servidor)
Desenvolvido em ASP.NET Core MVC com .NET 8, funciona como o núcleo de governança e controle.

* **Banco de Dados:** Utiliza SQLite gerenciado via Entity Framework Core, garantindo portabilidade e facilidade de implementação.
* **Comunicação em Tempo Real:** Implementação de WebSockets através do SignalR para o chat de suporte e notificações instantâneas.
* **Segurança de Configuração:** As credenciais e strings de conexão são protegidas através da leitura de arquivos JSON embutidos como recursos do sistema (Embedded Resources).
* **Autenticação:** Sistema baseado em Cookies com suporte para diferentes níveis de acesso (Admin, Coordenador, Colaborador, Diretoria/RH).
* **Suporte a Proxy Reverso:** Configuração nativa de *ForwardedHeaders* para garantir a interoperabilidade e segurança ao operar atrás de proxies reversos como Nginx ou Apache.

### 2. Agente de Coleta (Cliente)
Aplicação de console desenvolvida em .NET 8, desenhada para execução em segundo plano nas estações de trabalho.

* **Servidor TCP Seguro:** Escuta na porta 27275 utilizando comunicação criptografada SSL. O certificado autoassinado é gerado em memória com recurso a uma chave RSA estática importada, assegurando um *Thumbprint* previsível. Esta abordagem previne ataques *Man-in-the-Middle* (MitM) ao permitir o "pinning" no servidor Web, contornando simultaneamente problemas de chaves efêmeras no Windows.
* **Telemetria Avançada:** Coleta de dados de hardware via WMI, incluindo estado do processador, memória RAM, armazenamento, fabricante e detalhes do sistema operacional.
* **Controle Remoto:** Capacidade de captura e *streaming* contínuo de tela, simulação de eventos de mouse e teclado, atalhos de sistema (como o envio nativo de *Ctrl+Alt+Del*), manipulação da área de transferência (*clipboard*) e execução de comandos de sistema.
* **Transferência de Arquivos:** Suporte para o upload direto e seguro de arquivos para a área de trabalho do usuário remoto.

### 3. Funcionalidades Principais

* **Gestão de Ativos Diversificados:** Registro detalhado de computadores, monitores, smartphones, periféricos e infraestrutura de rede.
* **Centro de Suporte (Help Desk):** Sistema de tickets com histórico de conversas, anexos e priorização de chamados.
* **Monitoramento Ativo:** Serviço em background para verificação de disponibilidade (Ping) dos ativos de rede.
* **Auditoria e Manutenção:** Registro histórico de intervenções técnicas (hardware e software) e logs persistentes de ações administrativas.

### 4. Guia de Compilação e Implementação

### Publicação do Painel Web
Para gerar o executável do servidor sem dependências externas de runtime:

dotnet publish Web/Web.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

### Publicação do Agente (Coleta)
O agente deve ser compilado e distribuído para as máquinas alvo:

dotnet publish Coleta/Coleta.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

### 5. Requisitos de Rede e Segurança
* **Firewall:** É obrigatória a abertura da porta TCP 27275 nas máquinas clientes para permitir a recepção de comandos do painel central.

* **Segurança de Endpoint:** Devido às capacidades de controle remoto (injeção de teclas e captura de tela), o executável do agente pode ser identificado como um falso positivo por soluções antivírus. Deve ser adicionado à lista de exclusões da política de segurança corporativa.

* **HTTPS:** O servidor está configurado para suportar ambientes de produção, sendo recomendada a utilização de certificados válidos para a interface Web.

### 6. Estrutura de Dados
O sistema utiliza um esquema SQL organizado para garantir a integridade referencial entre colaboradores e seus respectivos equipamentos, permitindo uma rastreabilidade total desde a entrega de um periférico até o histórico de manutenção de um computador.
