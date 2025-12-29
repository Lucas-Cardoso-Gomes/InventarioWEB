using System;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Management;
using Microsoft.Extensions.Configuration;
using Coleta.Models;
using Coleta;
using System.Threading.Tasks;

namespace coleta
{
    partial class Program
    {
        static async Task Main()
        {
            // Carregar configurações do appsettings.json
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            IConfiguration config = builder.Build();

            string solicitarInformacoes = config["Autenticacao:SolicitarInformacoes"];
            string realizarComandos = config["Autenticacao:RealizarComandos"];
            string encryptionKey = config["Autenticacao:EncryptionKey"];

            if (string.IsNullOrEmpty(encryptionKey))
            {
                Console.WriteLine("[ERROR] 'EncryptionKey' not found in appsettings. Terminating.");
                return;
            }

            Console.Clear();

            try
            {
                int port = 27275;
                TcpListener listener = new TcpListener(IPAddress.Any, port);
                listener.Start();
                Console.WriteLine($"Aguardando solicitações na porta {port}...");
                Console.WriteLine("Certifique-se de que a porta 27275 está liberada no firewall.");

                while (true)
                {
                    TcpClient client = null;
                    try
                    {
                        client = listener.AcceptTcpClient();
                        using (NetworkStream stream = client.GetStream())
                        using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                        using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
                        {
                            string clientIP = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
                            Console.WriteLine($"[INFO] Conexão recebida do IP: {clientIP}");

                            string encryptedAuth = reader.ReadLine();
                            if (encryptedAuth == null)
                            {
                                Console.WriteLine("[WARN] O cliente desconectou antes de enviar a autenticação.");
                                continue;
                            }

                            string autenticacao;
                            try
                            {
                                autenticacao = EncryptionHelper.Decrypt(encryptedAuth, encryptionKey);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[ERROR] Falha na descriptografia da autenticação: {ex.Message}");
                                writer.WriteLine(EncryptionHelper.Encrypt("Authentication Failed", encryptionKey));
                                continue;
                            }

                            autenticacao = autenticacao.Trim();
                            Console.WriteLine($"[DEBUG] Autênticação recebida (Decrypted): '{autenticacao}'");

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
                                writer.WriteLine(EncryptionHelper.Encrypt(resposta, encryptionKey));
                                Console.WriteLine($"[INFO] Informações enviadas para o IP: {clientIP}");
                            }
                            else if (autenticacao == realizarComandos)
                            {
                                string encryptedCommand = reader.ReadLine();
                                if (encryptedCommand == null)
                                {
                                    Console.WriteLine("[WARN] O cliente desconectou antes de enviar o comando.");
                                    continue;
                                }

                                string comandoRemoto;
                                try
                                {
                                    comandoRemoto = EncryptionHelper.Decrypt(encryptedCommand, encryptionKey);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"[ERROR] Falha na descriptografia do comando: {ex.Message}");
                                    writer.WriteLine(EncryptionHelper.Encrypt("Command Decryption Failed", encryptionKey));
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

                                        // Encrypt the screenshot data
                                        // Since the original protocol sent length then raw base64,
                                        // we will just send the encrypted base64 string as a single line or similarly.
                                        // To preserve "length then content" logic if client expects it, we can encrypt the content.
                                        // However, encryption adds IV and padding, changing length.
                                        // Simpler: Encrypt the whole base64 string and send it as a line.

                                        string encryptedScreenshot = EncryptionHelper.Encrypt(base64String, encryptionKey);

                                        // The client expects a single response line for decryption.
                                        // We remove the length prefix because the new protocol is line-based encrypted Base64.
                                        writer.WriteLine(encryptedScreenshot);
                                        await writer.FlushAsync();

                                        Console.WriteLine($"[INFO] Screenshot enviado com sucesso (Encrypted size: {encryptedScreenshot.Length}).");
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"[ERROR] Falha ao capturar a tela: {ex.Message}");
                                        writer.WriteLine(EncryptionHelper.Encrypt($"Error: {ex.Message}", encryptionKey));
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
                                    writer.WriteLine(EncryptionHelper.Encrypt("Mouse event handled.", encryptionKey));
                                }
                                else if (comandoRemoto.StartsWith("set_clipboard"))
                                {
                                    var text = comandoRemoto.Substring("set_clipboard".Length).Trim();
                                    RemoteControl.SetClipboardText(text);
                                    writer.WriteLine(EncryptionHelper.Encrypt("Clipboard set.", encryptionKey));
                                }
                                else if (comandoRemoto == "get_clipboard")
                                {
                                    var text = RemoteControl.GetClipboardText();
                                    writer.WriteLine(EncryptionHelper.Encrypt(text, encryptionKey));
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
                                        writer.WriteLine(EncryptionHelper.Encrypt("Keyboard event handled.", encryptionKey));
                                    }
                                }
                                else if (comandoRemoto == "send_ctrl_alt_del")
                                {
                                    try
                                    {
                                        Process.Start("taskmgr.exe");
                                        writer.WriteLine(EncryptionHelper.Encrypt("Ctrl+Alt+Del sent.", encryptionKey));
                                    }
                                    catch (Exception ex)
                                    {
                                        writer.WriteLine(EncryptionHelper.Encrypt($"Error: {ex.Message}", encryptionKey));
                                    }
                                }
                                else if (comandoRemoto.StartsWith("upload_file"))
                                {
                                    // NOTE: File upload encryption is tricky with raw streams.
                                    // For now, we will assume the file content stream is NOT encrypted,
                                    // only the command.
                                    // To encrypt file content, we would need to read encrypted chunks.
                                    // Given constraints, we'll keep raw transfer for file content or
                                    // explicitly wrap it.
                                    // Let's implement basic protection: The command is encrypted, so an attacker
                                    // cannot easily trigger this. But the payload is plain.
                                    // TODO: Implement Encrypted File Stream if needed.

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
                                            var read = await stream.ReadAsync(buffer, 0, (int)Math.Min(buffer.Length, fileSize - bytesRead));
                                            if (read == 0) break;
                                            await fileStream.WriteAsync(buffer, 0, read);
                                            bytesRead += read;
                                        }
                                    }
                                    writer.WriteLine(EncryptionHelper.Encrypt("File uploaded successfully.", encryptionKey));
                                }
                                else
                                {
                                    string resultadoComando = Comandos.ExecutarComando(comandoRemoto);
                                    writer.WriteLine(EncryptionHelper.Encrypt(resultadoComando, encryptionKey));
                                    Console.WriteLine($"[INFO] Resultado do comando enviado para o IP: {clientIP}");
                                }
                            }
                            else
                            {
                                string resposta = "Código de Autenticação Incorreto!";
                                writer.WriteLine(EncryptionHelper.Encrypt(resposta, encryptionKey));
                                Console.WriteLine($"[FAIL] Falha no código de autenticação para o IP: {clientIP}. Enviado: '{autenticacao}'");
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