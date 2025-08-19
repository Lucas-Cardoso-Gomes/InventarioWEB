using System;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Management;
using Microsoft.Extensions.Configuration;
using Coleta.Models;

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
                int port = 27275;
                TcpListener listener = new TcpListener(IPAddress.Any, port);
                listener.Start();
                Console.WriteLine($"Aguardando solicitações na porta {port}...");
                Console.WriteLine("Certifique-se de que a porta 27275 está liberada no firewall.");

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
                    string comandoRemoto = dadosColetados.Length > 1 ? dadosColetados[1].Trim() : "";


                    if (autenticacao == solicitarInformacoes)
                    {
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
                Console.WriteLine("Erro: " + ex.Message);
            }
            Console.WriteLine("Fora do Loop, sistema finalizando operações...");
        }
    }
}