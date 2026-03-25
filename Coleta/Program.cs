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
            // Gerar um certificado auto-assinado em memória contornando o erro de chaves efêmeras no Windows
            X509Certificate2 serverCertificate;
            using (RSA rsa = RSA.Create(2048))
            {
                var request = new CertificateRequest("cn=ColetaAgent", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                using (X509Certificate2 ephemeralCert = request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(10)))
                {
                    // Exporta para PFX e re-importa para evitar o erro "platform does not support ephemeral keys"
                    // Sem usar PersistKeySet e MachineKeySet para evitar o vazamento de chaves no disco rígido a cada inicialização
                    byte[] pfxData = ephemeralCert.Export(X509ContentType.Pfx, "temp_password");
                    serverCertificate = new X509Certificate2(pfxData, "temp_password");
                }
            }

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
                        client = listener.AcceptTcpClient();
                        string clientIP = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
                        Console.WriteLine($"[INFO] Conexão recebida do IP: {clientIP}");

                        using (NetworkStream networkStream = client.GetStream())
                        using (SslStream sslStream = new SslStream(networkStream, false))
                        {
                            // Autenticar como servidor usando o certificado gerado em memória
                            await sslStream.AuthenticateAsServerAsync(serverCertificate, clientCertificateRequired: false, checkCertificateRevocation: true);

                            using (StreamReader reader = new StreamReader(sslStream, Encoding.UTF8))
                            using (StreamWriter writer = new StreamWriter(sslStream, Encoding.UTF8) { AutoFlush = true })
                            {

                            string autenticacao = reader.ReadLine();
                            if (autenticacao == null)
                            {
                                Console.WriteLine("[WARN] O cliente desconectou antes de enviar a autenticação.");
                                continue;
                            }

                            autenticacao = autenticacao.Trim();
                            Console.WriteLine($"[DEBUG] Autênticação recebida: '{autenticacao}'");

                            if (autenticacao == solicitarInformacoes)
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
                                    Armazenamento = Armazenamento.GetStorageInfo()
                                };

                                string resposta = JsonSerializer.Serialize(hardwareInfo);
                                writer.WriteLine(resposta);
                                Console.WriteLine($"[INFO] Informações enviadas para o IP: {clientIP}");
                            }
                            else if (autenticacao == realizarComandos)
                            {
                                string comandoRemoto = reader.ReadLine();
                                if (comandoRemoto == null)
                                {
                                    Console.WriteLine("[WARN] O cliente desconectou antes de enviar o comando.");
                                    continue;
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
                                        writer.WriteLine(base64String.Length);
                                        writer.Write(base64String);
                                        await writer.FlushAsync();

                                        Console.WriteLine($"[INFO] Screenshot enviado com sucesso ({base64String.Length} caracteres).");
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"[ERROR] Falha ao capturar a tela: {ex.Message}");
                                        writer.WriteLine($"Error: {ex.Message}");
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
                                    writer.WriteLine("Mouse event handled.");
                                }
                                else if (comandoRemoto.StartsWith("set_clipboard"))
                                {
                                    var text = comandoRemoto.Substring("set_clipboard".Length).Trim();
                                    RemoteControl.SetClipboardText(text);
                                    writer.WriteLine("Clipboard set.");
                                }
                                else if (comandoRemoto == "get_clipboard")
                                {
                                    var text = RemoteControl.GetClipboardText();
                                    writer.WriteLine(text);
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
                                        writer.WriteLine("Keyboard event handled.");
                                    }
                                }
                                else if (comandoRemoto == "send_ctrl_alt_del")
                                {
                                    try
                                    {
                                        Process.Start("taskmgr.exe");
                                        writer.WriteLine("Ctrl+Alt+Del sent.");
                                    }
                                    catch (Exception ex)
                                    {
                                        writer.WriteLine($"Error: {ex.Message}");
                                    }
                                }
                                else if (comandoRemoto.StartsWith("upload_file"))
                                {
                                    var parts = comandoRemoto.Split(' ');
                                    var fileName = parts[1];
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
                                    writer.WriteLine("File uploaded successfully.");
                                }
                                else
                                {
                                    string resultadoComando = Comandos.ExecutarComando(comandoRemoto);
                                    writer.WriteLine(resultadoComando);
                                    Console.WriteLine($"[INFO] Resultado do comando enviado para o IP: {clientIP}");
                                }
                            }
                            else
                            {
                                string resposta = "Código de Autenticação Incorreto!";
                                writer.WriteLine(resposta);
                                Console.WriteLine($"[FAIL] Falha no código de autenticação para o IP: {clientIP}. Enviado: '{autenticacao}'");
                            }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] Erro ao manusear cliente: {ex.Message}");
                    }
                    finally
                    {
                        client?.Close();
                        Console.WriteLine("[INFO] Conexão fechada. Aguardando novas instruções...");
                        Console.WriteLine("----------------------------------------------------");
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