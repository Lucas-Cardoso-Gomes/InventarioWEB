# Instruções de Instalação e Configuração

Este documento fornece as instruções necessárias para configurar e executar a aplicação de coleta de dados.

## Configuração do Firewall

O agente `Coleta` escuta na porta **TCP 27275** por conexões de entrada. Para que a aplicação `Chamada` (ou a aplicação `Web` futuramente) possa se comunicar com os agentes `Coleta` instalados nas máquinas da rede, é necessário criar uma regra de firewall em cada máquina cliente para permitir o tráfego de entrada nesta porta.

### Criando a Regra no Firewall do Windows (com privilégios de Administrador)

Você pode criar a regra de firewall executando o seguinte comando no **PowerShell** ou no **Prompt de Comando (CMD)** como Administrador:

```powershell
netsh advfirewall firewall add rule name="Permitir Conexao Coleta" dir=in action=allow protocol=TCP localport=27275
```

**O que este comando faz:**

*   `netsh advfirewall firewall add rule`: Comando para adicionar uma nova regra no Firewall do Windows com Segurança Avançada.
*   `name="Permitir Conexao Coleta"`: Define um nome claro e descritivo para a regra.
*   `dir=in`: Especifica que a regra se aplica ao tráfego de entrada (`inbound`).
*   `action=allow`: Especifica que a ação da regra é permitir (`allow`) a conexão.
*   `protocol=TCP`: Define o protocolo como TCP.
*   `localport=27275`: Especifica a porta local que será liberada.

Após executar este comando, o agente `Coleta` deverá ser capaz de receber solicitações da aplicação principal.
