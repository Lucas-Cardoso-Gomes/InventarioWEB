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
                                        writer.WriteLine(base64String);
                                        Console.WriteLine("[INFO] Screenshot sent successfully.");
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"[ERROR] Failed to take screenshot: {ex.Message}");
                                        writer.WriteLine($"Error: {ex.Message}");
                                    }
                                }
                                else if (comandoRemoto.StartsWith("mouse_event"))
                                {
                                    var parts = comandoRemoto.Split(' ');
                                    int x = int.Parse(parts[1]);
                                    int y = int.Parse(parts[2]);
                                    string type = parts[3];

                                    RemoteControl.SetCursorPos(x, y);

                                    if (type == "click")
                                    {
                                        RemoteControl.mouse_event(RemoteControl.MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                                        RemoteControl.mouse_event(RemoteControl.MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                                    }
                                    writer.WriteLine("Mouse event handled.");
                                }
                                else if (comandoRemoto.StartsWith("keyboard_event"))
                                {
                                    var key = comandoRemoto.Split(' ')[1];
                                    // This is a simplified mapping. A more complete solution would handle more keys.
                                    byte vk = (byte)char.ToUpper(key[0]);
                                    RemoteControl.keybd_event(vk, 0, 0, 0);
                                    RemoteControl.keybd_event(vk, 0, 0x02, 0); // KEYEVENTF_KEYUP
                                    writer.WriteLine("Keyboard event handled.");
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