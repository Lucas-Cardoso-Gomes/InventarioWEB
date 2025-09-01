InventarioWEB
Visão Geral
O InventarioWEB é um sistema de gerenciamento de inventário de TI projetado para coletar informações detalhadas sobre hardware e software de computadores em uma rede, além de gerenciar colaboradores, periféricos, monitores e manutenções. A solução é composta por dois componentes principais: um agente de coleta de dados (Coleta) e uma aplicação web (Web) para visualização e gerenciamento dos dados.

Funcionalidades

Coleta de Dados
Agente de Coleta: Um aplicativo de console que é executado nas máquinas clientes.

Coleta Abrangente de Hardware: Coleta informações sobre:

Processador (Fabricante, Modelo, Cores, Threads, Clock)
Memória RAM (Tamanho, Tipo, Velocidade, Voltagem)
Armazenamento (Discos, Espaço Total, Espaço Livre)
Informações do Sistema Operacional
Endereço MAC
Fabricante do Computador
Consumo de CPU
Usuário Logado

Comunicação Segura: Utiliza autenticação baseada em chaves para comunicação entre o agente e o servidor.

Execução Remota de Comandos: Permite a execução de comandos remotamente nos clientes para fins de gerenciamento.

Aplicação Web
Dashboard Centralizado: Interface web para visualização de todos os dados coletados.

Gerenciamento de Ativos:

Computadores: Lista e detalha todas as máquinas inventariadas.
Colaboradores: Gerencia informações dos colaboradores e os ativos a eles associados.
Monitores e Periféricos: Cadastra e associa monitores e periféricos aos colaboradores.
Controle de Acesso: Sistema de autenticação e autorização com diferentes níveis de acesso (Admin, Coordenador, etc.).
Manutenções: Registra o histórico de manutenções de hardware e software dos equipamentos.
Logs: Mantém logs detalhados de eventos do sistema e ações dos usuários.
Exportação de Dados: Permite a exportação dos dados de inventário.

Arquitetura
O sistema é dividido em duas partes principais:

Coleta: Uma aplicação de console .NET que atua como um agente. Ele é instalado ou executado nas máquinas clientes. O agente abre um listener TCP na porta 27275 e aguarda conexões da aplicação web. Com base na chave de autenticação recebida, ele pode retornar informações detalhadas do hardware ou executar um comando remoto.

Web: Uma aplicação web ASP.NET Core MVC que fornece a interface de usuário para gerenciar o inventário. Ela se comunica com os agentes de coleta para obter dados em tempo real e os armazena em um banco de dados SQL Server para persistência e consulta. A aplicação também gerencia todas as outras entidades do sistema, como colaboradores, monitores e periféricos.

Esquema do Banco de Dados
O banco de dados, chamado Coletados, armazena todas as informações do inventário. As principais tabelas são:

Usuarios: Armazena os usuários do sistema web, suas credenciais e perfis de acesso.
Colaboradores: Contém os dados dos colaboradores da empresa.
Computadores: Armazena as informações de hardware coletadas dos computadores. A chave primária é o endereço MAC.
Monitores: Tabela para cadastro de monitores.
Perifericos: Tabela para cadastro de periféricos (teclados, mouses, etc.).
Manutencoes: Registra o histórico de manutenções realizadas nos ativos.
Logs: Logs de eventos da aplicação.
PersistentLogs: Logs persistentes de ações importantes realizadas pelos usuários.

Tecnologias Utilizadas
Backend: C#, .NET, ASP.NET Core MVC

Frontend: HTML, CSS, JavaScript, Bootstrap

Banco de Dados: Microsoft SQL Server

Comunicação: TCP/IP Sockets para comunicação entre o agente e o servidor.

Frameworks/Bibliotecas:

Entity Framework Core (implícito para acesso a dados)

System.Management para coleta de informações do WMI no Windows.

Instalação e Configuração
Pré-requisitos
.NET SDK

Microsoft SQL Server

Um ambiente Windows para o agente de coleta.

Banco de Dados
Execute o script schema_completo.sql em seu servidor SQL Server para criar o banco de dados Coletados e todas as tabelas necessárias.

Configure a string de conexão no arquivo appsettings.json da aplicação Web.

Aplicação Web
Abra a solução Web/Web.sln no Visual Studio.

Configure as chaves de autenticação no arquivo appsettings.json. Estas chaves devem ser as mesmas configuradas no agente Coleta.

JSON

"Autenticacao": {
  "SolicitarInformacoes": "SUA_CHAVE_PARA_INFO",
  "RealizarComandos": "SUA_CHAVE_PARA_COMANDOS"
}
Compile e execute o projeto. A aplicação estará disponível em http://localhost:80 e https://localhost:443.

Agente de Coleta
Abra a solução Coleta/Coleta.sln no Visual Studio.

Configure as mesmas chaves de autenticação no arquivo appsettings.json.

JSON

"Autenticacao": {
  "SolicitarInformacoes": "SUA_CHAVE_PARA_INFO",
  "RealizarComandos": "SUA_CHAVE_PARA_COMANDOS"
}
Compile o projeto para gerar o executável.

Distribua e execute o Coleta.exe nas máquinas que você deseja inventariar.

Certifique-se de que a porta 27275 esteja liberada no firewall das máquinas clientes para permitir a comunicação com o servidor web.

Utilização
Acesse a aplicação web através do seu navegador.

Faça login com o usuário administrador padrão (admin/admin).

Navegue até a seção de gerenciamento para visualizar os computadores online e solicitar a coleta de dados.

Cadastre colaboradores, monitores e periféricos conforme necessário.

Associe os ativos aos seus respectivos colaboradores para um controle completo do inventário.