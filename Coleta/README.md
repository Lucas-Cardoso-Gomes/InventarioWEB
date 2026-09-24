# 🖥️ Agente de Coleta (Coleta Agent)

O **Agente de Coleta** é um componente de endpoint desenvolvido em **.NET 8.0** para sistemas operacionais Microsoft Windows. Ele atua de forma autônoma como um serviço/processo em segundo plano, encarregado de realizar a **telemetria em tempo real de hardware e software**, além de disponibilizar uma **interface de suporte e controle remoto criptografada e de alta performance**.

---

## 🏗 Arquitetura e Segurança

### 1. Comunicação Servidor TCP SSL (Porta 27275)
O agente atua como um servidor TCP seguro escutando na porta padrão **`27275`**.
* **TLS/SSL Criptografado:** Toda comunicação é cifrada via `SslStream`.
* **Certificate Pinning & Chave RSA Estática:** O agente gera um certificado SSL auto-assinado em memória utilizando uma chave RSA estática configurada em `Seguranca:CertificadoPrivateKey`. Isso permite que o Painel Web valide o *Thumbprint* do certificado (Certificate Pinning) e evite ataques *Man-in-the-Middle* (MitM).

### 2. Autenticação HMAC por Handshake Nonce
Para prevenir ataques de reprodução (*Replay Attacks*):
1. Quando o Painel Web se conecta via TCP, o agente envia imediatamente um **Nonce** único (Guid de uso único).
2. O servidor deve responder com um hash **HMAC-SHA256** derivado do Nonce e da chave secreta configurada (`SolicitarInformacoes` ou `RealizarComandos`).
3. O agente possui dois níveis de permissão independentes:
   * **`SolicitarInformacoes`:** Autoriza a leitura dos dados de telemetria do sistema.
   * **`RealizarComandos`:** Autoriza a execução de comandos remotos, suporte interativo, controle de entrada e streaming de tela.

### 3. Telemetria Ativa (Push Inicial no Startup)
Ao ser iniciado, o agente executa a função `SendInitialTelemetryAsync` em uma thread em segundo plano (*fire-and-forget*):
* Coleta o snapshot completo do hardware e SO.
* Envia uma requisição HTTP POST para o endpoint `/api/agent/telemetry` do Painel Web configurado em `Servidor:Url`.
* A requisição carrega o token Bearer derivado do hash da chave de autenticação.

---

## 📂 Módulos e Estrutura de Código

Abaixo está o detalhamento técnico de cada arquivo do projeto `Coleta`:

| Arquivo | Descrição e Funcionalidade |
| :--- | :--- |
| **`Program.cs`** | Ponto de entrada da aplicação (`Main`). Carrega as configurações embutidas (`Assembly.GetManifestResourceStream`), constrói o certificado SSL em memória, dispara o push de telemetria inicial e mantém o loop do servidor `TcpListener` na porta 27275. Gerencia a autenticação HMAC e faz o roteamento das requisições e comandos recebidos. |
| **`RemoteControl.cs`** | Módulo de controle remoto avançado. Interage com as APIs nativas do Windows (`user32.dll` via P/Invoke: `SetCursorPos`, `mouse_event`, `SendInput`, `OpenClipboard`, `SetClipboardText`, `GetClipboardText`). Realiza compensação e offsets para múltiplos monitores (`SM_XVIRTUALSCREEN`, `SM_YVIRTUALSCREEN`) e alinhamento de coordenadas de mouse e teclado. |
| **`ScreenCapturer.cs`** | Módulo nativo de captura de tela em alta performance. Utiliza GDI e User32 (`CreateDC`, `CreateCompatibleDC`, `CreateCompatibleBitmap`, `BitBlt`, `GetCursorInfo`, `DrawIcon`) para capturar o desktop incluindo a posição exata do cursor do mouse, convertendo o resultado em bytes JPEG otimizados. Gerencia rigorosamente o descarte de handles GDI (`DeleteObject`, `ReleaseDC`, `DeleteDC`). |
| **`Processador.cs`** | Realiza a leitura de especificações da CPU via WMI (`Win32_Processor`), como modelo, fabricante, núcleos, threads e clock. Também captura a temperatura em tempo real consultando `MSAcpi_ThermalZoneTemperature` ou contadores térmicos do sistema. |
| **`RAM.cs`** | Coleta informações da memória física via WMI (`Win32_PhysicalMemory` e `Win32_OperatingSystem`): capacidade total, memória utilizada, disponível, velocidade (MHz) e quantidade de slots ocupados. |
| **`Armazenamento.cs`** | Combina `System.IO.DriveInfo` com consultas WMI (`Win32_DiskDrive` e `Win32_LogicalDisk`) para enumerar discos físicos, modelos, tipos (SSD/HDD), letras de unidade, partições e espaço total/livre. |
| **`OS.cs`** | Obtém informações sobre o Sistema Operacional via `Win32_OperatingSystem` e `Environment.OSVersion`: Nome, Edição, Arquitetura (64-bit/32-bit), Versão da Build e Diretório do Sistema. |
| **`bateria.cs`** | Monitora o status da bateria em notebooks via WMI (`Win32_Battery`). Informa porcentagem atual, status de carregamento e calcula o **Wear Level** (desgaste da bateria) comparando `BatteryFullChargedCapacity` com `BatteryDesignCapacity`. |
| **`consumo.cs`** | Mede a porcentagem de utilização da CPU em tempo real utilizando contadores de desempenho do Windows (`PerformanceCounter("Processor", "% Processor Time", "_Total")`). |
| **`fabricante.cs`** | Obtém marca, modelo, número de série da máquina e UUID do sistema via WMI (`Win32_ComputerSystem` e `Win32_ComputerSystemProduct`). |
| **`uptime.cs`** | Calcula o tempo decorrido desde a última inicialização do sistema (Uptime) utilizando `Environment.TickCount64`. |
| **`User.cs`** | Identifica o usuário ativamente conectado no Windows (`Environment.UserName`, `Environment.UserDomainName`). |
| **`MAC.cs`** | Captura e formata o endereço MAC da interface de rede física ativa principal (`NetworkInterface.GetAllNetworkInterfaces()`). |
| **`KeyCodeConverter.cs`** | Tabela de conversão que mapeia identificadores de teclas enviados pelo navegador Web (ex: `"Enter"`, `"Backspace"`, `"ArrowLeft"`) para os seus respectivos códigos de tecla virtual do Windows (Virtual Key Codes - VK). |
| **`Models.cs`** | Define as classes de modelo DTO (`HardwareInfo`, `ProcessadorModel`, `RAMModel`, `UserModel`, `FabricanteModel`, `OSModel`, `ArmazenamentoModel`, `DiskInfo`, `BateriaModel`, `UptimeModel`, `SoftwareInfo`) utilizadas na serialização JSON. |
| **`Comandos.cs`** | Utilitário para execução silenciosa de comandos do sistema via CMD ou PowerShell (`ProcessStartInfo` redirecionando `StandardOutput` e `StandardError`). |

---

## 🎮 Comandos de Controle Remoto Suportados

Quando conectado ao agente com a credencial autorizada (`RealizarComandos`), o cliente Web pode enviar as seguintes instruções:

1. **`take_screenshot`:** Captura e retorna uma foto instantânea da tela em formato Base64.
2. **`take_screenshot_stream`:** Inicia um stream contínuo de vídeo em tempo real (envia pacotes contendo o tamanho do frame + bytes JPEG do frame).
3. **`mouse_event <tipo> <x> <y> <deltaY>`:** Injeta eventos de clique (`down_0`, `up_0`, `down_2`, `up_2`), movimentação (`move`) ou rolagem de scroll (`wheel`).
4. **`keyboard_event <tecla> <estado>`:** Injeta pressionamento (`down`) ou soltura (`up`) de teclas do teclado.
5. **`get_clipboard` / `set_clipboard <texto>`:** Lê ou altera o conteúdo da área de transferência (Ctrl+C / Ctrl+V) do Windows do usuário remoto.
6. **`send_ctrl_alt_del`:** Dispara a abertura do Gerenciador de Tarefas do Windows (`taskmgr.exe`).
7. **`upload_file <nome_arquivo> <tamanho>`:** Transfere um arquivo diretamente para a **Área de Trabalho** (Desktop) do usuário remoto.
8. **`get_installed_programs`:** Executa um script PowerShell temporário e seguro que varre o Registro do Windows (`HKLM` e `CU` - 32/64 bits) e integra com o CLI do **`winget`** para listar todos os softwares instalados com ID, versão e fornecedor.
9. **Comandos de Linha de Comando (Prompt/PowerShell):** Qualquer texto enviado que não corresponda às palavras-chave acima é executado no terminal e seu retorno retornado ao servidor.

---

## 🛠⚙️ Configuração (`appsettings.json`)

O arquivo de configuração do agente contém os parâmetros abaixo:

```json
{
  "Autenticacao": {
    "SolicitarInformacoes": "SUA_CHAVE_HMAC_INFO_AQUI",
    "RealizarComandos": "SUA_CHAVE_HMAC_COMANDOS_AQUI"
  },
  "Servidor": {
    "Url": "http://seu-servidor-web:5000"
  },
  "Seguranca": {
    "CertificadoPrivateKey": "SUA_CHAVE_PRIVADA_RSA_BASE64_AQUI"
  }
}
```

> ⚠️ **Nota:** Para maior segurança, o arquivo `appsettings.json` pode ser embutido diretamente como um recurso compilado (*Embedded Resource*) dentro do executável `.exe`.

---

## 🚀 Instalação e Implantação

### 1. Script de Instalação Automática (`install.bat`)
O repositório inclui o script `install.bat` para automatizar o deploy na estação de trabalho do cliente. Ele realiza as seguintes ações:
* Cria a pasta de destino em `C:\Program Files\ColetaAgent`.
* Copia o executável e arquivos necessários.
* Adiciona exceções de entrada na regra do **Windows Firewall** para a porta TCP `27275`.
* Configura uma Tarefa Agendada (*Scheduled Task*) do Windows para executar o agente automaticamente na inicialização do sistema com privilégios elevados (`SYSTEM` / `HIGHEST`).

### 2. Compilação para Produção
Para compilar o agente como um executável único e independente (*Self-Contained*) para Windows 64-bit:

```bash
dotnet publish Coleta/Coleta.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

O executável final estará disponível em `Coleta/bin/Release/net8.0/win-x64/publish/Coleta.exe`.
