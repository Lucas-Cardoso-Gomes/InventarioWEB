using System;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Text;
using System.Security.Cryptography;
using System.Management;
using Microsoft.Extensions.Configuration;

namespace coleta
{
    partial class Program
    {
        static void Main()
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
                AdicionarRegraFirewallPorta();
                AdicionarRegraFirewallPrograma();

                int port = 27275;
                TcpListener listener = new TcpListener(IPAddress.Any, port);
                listener.Start();
                Console.WriteLine($"Aguardando solicitações na porta {port}...");

                while (true)
                {
                    TcpClient client = listener.AcceptTcpClient();
                    NetworkStream stream = client.GetStream();

                    string clientIP = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
                    Console.WriteLine($"Conexão recebida do IP: {clientIP}");

                    byte[] buffer = new byte[5120];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    string dados = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    var dadosColetados = dados.Split('\n');
                    string autenticacao = dadosColetados[0].Trim();
                    string comandoRemoto = dadosColetados[1].Trim();

                    if (autenticacao == solicitarInformacoes)
                    {
                        string processador = Processador.GetProcessorInfo();
                        string ram = RAM.GetRamInfo();
                        string usuario = User.GetUserInfo();
                        string fabricante = Fabricante.GetManufacturer();
                        string mac = MAC.GetFormattedMacAddress();
                        string sistemaOperacional = OS.GetOSInfo();
                        string armazenamento = Armazenamento.GetStorageInfo();
                        string consumoCPU = Consumo.Uso();
                        string resposta = $"{processador}\n" +
                                            $"{ram}\n" +
                                            $"{usuario}\n" +
                                            $"{fabricante}\n" +
                                            $"{mac}\n" +
                                            $"{sistemaOperacional}\n" +
                                            $"{consumoCPU}\n" + 
                                            $"{armazenamento}";

                        //Console.WriteLine(resposta);

                        byte[] responseBytes = Encoding.UTF8.GetBytes(resposta);
                        stream.Write(responseBytes, 0, responseBytes.Length);

                        Console.WriteLine($"Informações enviadas para o IP: {clientIP}");
                        Console.WriteLine("Aguardando novas instruções...");

                        client.Close();
                    }
                    else if (autenticacao == realizarComandos)
                    {
                        string resultadoComando = Comandos.ExecutarComando(comandoRemoto);

                        byte[] responseBytes = Encoding.UTF8.GetBytes(resultadoComando);
                        stream.Write(responseBytes, 0, responseBytes.Length);

                        Console.WriteLine($"Informações enviadas para o IP: {clientIP}");
                        Console.WriteLine("Aguardando novas instruções...");

                        client.Close();
                    }
                    else
                    {
                        string resposta = "Código de Autenticação Incorreto!";

                        byte[] responseBytes = Encoding.UTF8.GetBytes(resposta);
                        stream.Write(responseBytes, 0, responseBytes.Length);

                        Console.WriteLine($"Falha no codigo de autenticação para o IP: {clientIP}");

                        client.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro ao inserir dados na tabela 'Computadores': " + ex.Message);
            }
            Console.WriteLine("Fora do Loop, sistema finalizando operações...");
        }

        static void AdicionarRegraFirewallPrograma()
        {
            try
            {
                string ruleName = "Liberação do programa Coleta";
                string appPath = @"C:\Coleta\coleta.exe";

                string command = $"advfirewall firewall add rule name=\"{ruleName}\" dir=in action=allow program=\"{appPath}\" enable=yes";

                ProcessStartInfo psi = new ProcessStartInfo("netsh", command)
                {
                    Verb = "runas",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (Process process = new Process())
                {
                    process.StartInfo = psi;
                    process.Start();

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    process.WaitForExit();

                    if (process.ExitCode == 0)
                    {
                        Console.WriteLine($"Regra do firewall adicionada com sucesso para o programa Coleta.");
                    }
                    else
                    {
                        Console.WriteLine($"Erro ao adicionar regra do firewall:");
                        Console.WriteLine($"Saída padrão: {output}");
                        Console.WriteLine($"Erro padrão: {error}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao adicionar regra do firewall: {ex.Message}");
            }
        }

        static void AdicionarRegraFirewallPorta()
        {
            try
            {
                int porta = 27275;
                string ruleName = $"Liberação da porta {porta}";

                string command = $"advfirewall firewall add rule name=\"{ruleName}\" dir=in action=allow protocol=TCP localport={porta}";

                ProcessStartInfo psi = new ProcessStartInfo("netsh", command)
                {
                    Verb = "runas",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (Process process = new Process())
                {
                    process.StartInfo = psi;
                    process.Start();

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    process.WaitForExit();

                    if (process.ExitCode == 0)
                    {
                        Console.WriteLine($"Regra do firewall adicionada com sucesso para a porta {porta}.");
                    }
                    else
                    {
                        Console.WriteLine($"Erro ao adicionar regra do firewall para a porta {porta}:");
                        Console.WriteLine($"Saída padrão: {output}");
                        Console.WriteLine($"Erro padrão: {error}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao adicionar regra do firewall para a porta: {ex.Message}");
            }
        }
    }
}