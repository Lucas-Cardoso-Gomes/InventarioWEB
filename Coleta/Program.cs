using System;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Diagnostics;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Management;
using Microsoft.Extensions.Configuration;
using Coleta.Models;
using Coleta;
using System.Threading.Tasks;
using System.Reflection;

namespace coleta
{
    partial class Program
    {
        static async Task Main()
        {
// --- CÓDIGO NOVO: Lendo o JSON da memória (Embedded Resource) ---
            var builder = new ConfigurationBuilder();
            var assembly = Assembly.GetExecutingAssembly();
            
            // O nome do recurso geralmente segue o padrão: NamespaceProjeto.NomeDoArquivo
            var appSettingsStream = assembly.GetManifestResourceStream("Coleta.appsettings.json");
            
            if (appSettingsStream != null)
            {
                builder.AddJsonStream(appSettingsStream);
            }
            else
            {
                Console.WriteLine("[ERROR] Falha de Segurança: Arquivo de configuração embutido não encontrado.");
                return; // Encerra a execução caso falhe, para não rodar sem autenticação
            }

            IConfiguration config = builder.Build();
            // -----------------------------------------------------------------

            // Gerar um certificado auto-assinado em memória contornando o erro de chaves efêmeras no Windows
            X509Certificate2 serverCertificate;
            using (RSA rsa = RSA.Create(2048))
            {
                // Import a static RSA private key to generate a self-signed certificate with a predictable Thumbprint
                // This allows the Web client to securely pin the Thumbprint and prevent MitM attacks.
                string staticPrivateKeyBase64 = config["Seguranca:CertificadoPrivateKey"] ?? "";
                if (!string.IsNullOrEmpty(staticPrivateKeyBase64))
                {
                    rsa.ImportRSAPrivateKey(Convert.FromBase64String(staticPrivateKeyBase64), out _);
                }

                var request = new CertificateRequest("cn=ColetaAgent", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                using (X509Certificate2 ephemeralCert = request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(10)))
                {
                    // Exporta para PFX e re-importa para evitar o erro "platform does not support ephemeral keys"
                    // Sem usar PersistKeySet e MachineKeySet para evitar o vazamento de chaves no disco rígido a cada inicialização
                    byte[] pfxData = ephemeralCert.Export(X509ContentType.Pfx, "temp_password");
                    serverCertificate = new X509Certificate2(pfxData, "temp_password");
                }
            }
            
            string solicitarInformacoes = config["Autenticacao:SolicitarInformacoes"];
            string realizarComandos = config["Autenticacao:RealizarComandos"];

            Console.Clear();

            try
            {
                int port = 27275;
                TcpListener listener = new TcpListener(IPAddress.Any, port);
                // Permite reuso da porta se estiver em estado TIME_WAIT
                listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                listener.Start();
                Console.WriteLine($"Aguardando solicitações na porta {port}...");
                Console.WriteLine("Certifique-se de que a porta 27275 está liberada no firewall.");

                while (true)
                {
                    TcpClient client = null;
                    try
                    {
                        client = await listener.AcceptTcpClientAsync();
                        string clientIP = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
                        Console.WriteLine($"[INFO] Conexão recebida do IP: {clientIP}");

                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                using (NetworkStream networkStream = client.GetStream())
                                using (SslStream sslStream = new SslStream(networkStream, false))
                                {
                                    // Autenticar como servidor usando o certificado gerado em memória
                                    await sslStream.AuthenticateAsServerAsync(serverCertificate, clientCertificateRequired: false, checkCertificateRevocation: true);

                                    using (StreamReader reader = new StreamReader(sslStream, Encoding.UTF8))
                                    using (StreamWriter writer = new StreamWriter(sslStream, Encoding.UTF8) { AutoFlush = true })
                                    {

                                        string nonce = Guid.NewGuid().ToString();
                                        await writer.WriteLineAsync(nonce);

                                        string autenticacaoHash = await reader.ReadLineAsync();
                                        if (autenticacaoHash == null)
                                        {
                                            Console.WriteLine("[WARN] O cliente desconectou antes de enviar a autenticação.");
                                            return;
                                        }

                                        autenticacaoHash = autenticacaoHash.Trim();

                                        string expectedHashSolicitar = Convert.ToBase64String(HMACSHA256.HashData(Encoding.UTF8.GetBytes(solicitarInformacoes), Encoding.UTF8.GetBytes(nonce)));
                                        string expectedHashComandos = Convert.ToBase64String(HMACSHA256.HashData(Encoding.UTF8.GetBytes(realizarComandos), Encoding.UTF8.GetBytes(nonce)));

                                        if (autenticacaoHash == expectedHashSolicitar)
                                        {
                                            Console.WriteLine("[INFO] Solicitação de informações recebida.");
                                            var hardwareInfo = new HardwareInfo
                                            {
                                                Processador = Processador.GetProcessorInfo(),
                                                Ram = RAM.GetRamInfo(),
                                                Usuario = User.GetUserInfo(),
                                                Fabricante = Fabricante.GetManufacturer(),
                                                MAC = MAC.GetFormattedMacAddress(),
                                                SO = OS.GetOSInfo(),
                                                ConsumoCPU = Consumo.Uso(),
                                                Armazenamento = Armazenamento.GetStorageInfo(),
                                                BateriaWearLevel = Bateria.GetWearLevel(),
                                                TempoAtividade = Uptime.GetUptime()
                                            };

                                            string resposta = JsonSerializer.Serialize(hardwareInfo);
                                            await writer.WriteLineAsync(resposta);
                                            Console.WriteLine($"[INFO] Informações enviadas para o IP: {clientIP}");
                                        }
                                        else if (autenticacaoHash == expectedHashComandos)
                                        {
                                            string comandoRemoto = await reader.ReadLineAsync();
                                            if (comandoRemoto == null)
                                            {
                                                Console.WriteLine("[WARN] O cliente desconectou antes de enviar o comando.");
                                                return;
                                            }

                                            comandoRemoto = comandoRemoto.Trim();
                                            Console.WriteLine($"[INFO] Solicitação de comando recebida: '{comandoRemoto}'");

                                            if (comandoRemoto == "take_screenshot")
                                            {
                                                try
                                                {
                                                    byte[] screenshotBytes = ScreenCapturer.CaptureScreen();
                                                    string base64String = Convert.ToBase64String(screenshotBytes);

                                                    // Envia o tamanho primeiro e depois os dados para garantir a integridade
                                                    await writer.WriteLineAsync(base64String.Length.ToString());
                                                    await writer.WriteAsync(base64String);
                                                    await writer.FlushAsync();

                                                    Console.WriteLine($"[INFO] Screenshot enviado com sucesso ({base64String.Length} caracteres).");
                                                }
                                                catch (Exception ex)
                                                {
                                                    Console.WriteLine($"[ERROR] Falha ao capturar a tela: {ex.Message}");
                                                    await writer.WriteLineAsync($"Error: {ex.Message}");
                                                }
                                            }
                                            else if (comandoRemoto == "take_screenshot_stream")
                                            {
                                                Console.WriteLine($"[INFO] Iniciando stream de tela contínuo...");
                                                try
                                                {
                                                    while (true)
                                                    {
                                                        byte[] screenshotBytes = ScreenCapturer.CaptureScreen();
                                                        byte[] sizeBytes = BitConverter.GetBytes(screenshotBytes.Length);
                                                        await sslStream.WriteAsync(sizeBytes, 0, 4);
                                                        await sslStream.WriteAsync(screenshotBytes, 0, screenshotBytes.Length);
                                                        await sslStream.FlushAsync();
                                                    }
                                                }
                                                catch (Exception)
                                                {
                                                    Console.WriteLine($"[INFO] Stream de tela encerrado ou desconectado.");
                                                }
                                            }
                                            else if (comandoRemoto.StartsWith("mouse_event"))
                                            {
                                                var parts = comandoRemoto.Split(' ');
                                                var type = parts[1];
                                                int x = int.Parse(parts[2]);
                                                int y = int.Parse(parts[3]);
                                                int deltaY = int.Parse(parts[4]);

                                                RemoteControl.HandleMouseEvent(type, x, y, deltaY);
                                                await writer.WriteLineAsync("Mouse event handled.");
                                            }
                                            else if (comandoRemoto.StartsWith("set_clipboard"))
                                            {
                                                var text = comandoRemoto.Substring("set_clipboard".Length).Trim();
                                                RemoteControl.SetClipboardText(text);
                                                await writer.WriteLineAsync("Clipboard set.");
                                            }
                                            else if (comandoRemoto == "get_clipboard")
                                            {
                                                var text = RemoteControl.GetClipboardText();
                                                await writer.WriteLineAsync(text);
                                            }
                                            else if (comandoRemoto.StartsWith("keyboard_event"))
                                            {
                                                var parts = comandoRemoto.Split(' ');
                                                if (parts.Length >= 3)
                                                {
                                                    var key = parts[1];
                                                    var state = parts[2];
                                                    var vkCode = KeyCodeConverter.GetVirtualKeyCode(key);
                                                    if (vkCode != 0)
                                                    {
                                                        RemoteControl.SendKeyEvent(vkCode, state == "up");
                                                    }
                                                    else
                                                    {
                                                        Console.WriteLine($"[WARN] Key not found in map: {key}");
                                                    }
                                                    await writer.WriteLineAsync("Keyboard event handled.");
                                                }
                                            }
                                            else if (comandoRemoto == "send_ctrl_alt_del")
                                            {
                                                try
                                                {
                                                    Process.Start("taskmgr.exe");
                                                    await writer.WriteLineAsync("Ctrl+Alt+Del sent.");
                                                }
                                                catch (Exception ex)
                                                {
                                                    await writer.WriteLineAsync($"Error: {ex.Message}");
                                                }
                                            }
                                            else if (comandoRemoto.StartsWith("upload_file"))
                                            {
                                                var parts = comandoRemoto.Split(' ');
                                                var fileName = Path.GetFileName(parts[1]); // Prevent Path Traversal
                                                var fileSize = long.Parse(parts[2]);
                                                var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                                                var filePath = Path.Combine(desktopPath, fileName);

                                                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                                                {
                                                    var buffer = new byte[8192];
                                                    long bytesRead = 0;
                                                    while (bytesRead < fileSize)
                                                    {
                                                        var read = await sslStream.ReadAsync(buffer, 0, (int)Math.Min(buffer.Length, fileSize - bytesRead));
                                                        if (read == 0) break;
                                                        await fileStream.WriteAsync(buffer, 0, read);
                                                        bytesRead += read;
                                                    }
                                                }
                                                await writer.WriteLineAsync("File uploaded successfully.");
                                            }
                                            else
                                            {
                                                string resultadoComando = Comandos.ExecutarComando(comandoRemoto);
                                                await writer.WriteLineAsync(resultadoComando);
                                                Console.WriteLine($"[INFO] Resultado do comando enviado para o IP: {clientIP}");
                                            }
                                        }
                                        else
                                        {
                                            string resposta = "Código de Autenticação Incorreto!";
                                            await writer.WriteLineAsync(resposta);
                                            Console.WriteLine($"[FAIL] Falha no código de autenticação para o IP: {clientIP}. Enviado: '{autenticacaoHash}'");
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[ERROR] Erro ao manusear cliente {clientIP}: {ex.Message}");
                            }
                            finally
                            {
                                client?.Close();
                                Console.WriteLine($"[INFO] Conexão fechada ({clientIP}). Aguardando novas instruções...");
                                Console.WriteLine("----------------------------------------------------");
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] Erro ao aceitar cliente: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro: " + ex.Message);
            }
            Console.WriteLine("Fora do Loop, sistema finalizando operações...");
        }
    }
}