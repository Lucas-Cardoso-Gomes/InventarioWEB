using System;
using System.Data;
using System.Data.SqlClient;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Chamada.Models;

namespace ColetaDados
{
    public class Chamada
    {
        private static IConfiguration Configuration;

        public static void Initialize(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public static void ColetaBD(string computador, string connectionString)
        {
            string computadorIp = computador;
            int serverPort = 27275;

            try
            {
                using (TcpClient client = new TcpClient())
                {
                    var connectTask = client.ConnectAsync(computadorIp, serverPort);
                    if (!connectTask.Wait(TimeSpan.FromSeconds(5))) // 5 second timeout
                    {
                        Console.WriteLine($"Timeout ao conectar com: {computadorIp}");
                        return;
                    }

                    using (NetworkStream stream = client.GetStream())
                    {
                        string autenticacao = Configuration["Autenticacao"];
                        byte[] data = Encoding.UTF8.GetBytes(autenticacao);

                        stream.Write(data, 0, data.Length);
                        Console.WriteLine($"Solicitação enviada ao computador: {computadorIp}");

                        byte[] buffer = new byte[8192];
                        int bytesRead = stream.Read(buffer, 0, buffer.Length);
                        string resposta = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                        Console.WriteLine($"Resposta recebida do computador: {computadorIp}");

                        var hardwareInfo = JsonSerializer.Deserialize<HardwareInfo>(resposta);

                        if (hardwareInfo == null || hardwareInfo.MAC == null)
                        {
                            Console.WriteLine($"Falha ao deserializar a resposta ou MAC é nulo para o IP: {computadorIp}");
                            return;
                        }

                        using (SqlConnection connection = new SqlConnection(connectionString))
                        {
                            connection.Open();

                            string mergeQuery = @"
                                MERGE INTO Computadores AS target
                                USING (VALUES (@MAC)) AS source (MAC)
                                ON target.MAC = source.MAC
                                WHEN MATCHED THEN
                                    UPDATE SET
                                        IP = @IP,
                                        Processador = @Processador,
                                        ProcessadorFabricante = @ProcessadorFabricante,
                                        ProcessadorCore = @ProcessadorCore,
                                        ProcessadorThread = @ProcessadorThread,
                                        ProcessadorClock = @ProcessadorClock,
                                        Ram = @Ram,
                                        RamTipo = @RamTipo,
                                        RamVelocidade = @RamVelocidade,
                                        RamVoltagem = @RamVoltagem,
                                        RamPorModule = @RamPorModule,
                                        Usuario = @Usuario,
                                        Hostname = @Hostname,
                                        Fabricante = @Fabricante,
                                        SO = @SO,
                                        ArmazenamentoC = @ArmazenamentoC,
                                        ArmazenamentoCTotal = @ArmazenamentoCTotal,
                                        ArmazenamentoCLivre = @ArmazenamentoCLivre,
                                        ArmazenamentoD = @ArmazenamentoD,
                                        ArmazenamentoDTotal = @ArmazenamentoDTotal,
                                        ArmazenamentoDLivre = @ArmazenamentoDLivre,
                                        ConsumoCPU = @ConsumoCPU,
                                        DataColeta = @DataColeta
                                WHEN NOT MATCHED THEN
                                    INSERT (MAC, IP, Processador, ProcessadorFabricante, ProcessadorCore, ProcessadorThread, ProcessadorClock, Ram, RamTipo, RamVelocidade, RamVoltagem, RamPorModule, Usuario, Hostname, Fabricante, SO, ArmazenamentoC, ArmazenamentoCTotal, ArmazenamentoCLivre, ArmazenamentoD, ArmazenamentoDTotal, ArmazenamentoDLivre, ConsumoCPU, DataColeta)
                                    VALUES (@MAC, @IP, @Processador, @ProcessadorFabricante, @ProcessadorCore, @ProcessadorThread, @ProcessadorClock, @Ram, @RamTipo, @RamVelocidade, @RamVoltagem, @RamPorModule, @Usuario, @Hostname, @Fabricante, @SO, @ArmazenamentoC, @ArmazenamentoCTotal, @ArmazenamentoCLivre, @ArmazenamentoD, @ArmazenamentoDTotal, @ArmazenamentoDLivre, @ConsumoCPU, @DataColeta);
                            ";

                            using (SqlCommand cmd = new SqlCommand(mergeQuery, connection))
                            {
                                cmd.Parameters.Add("@MAC", SqlDbType.NVarChar).Value = hardwareInfo.MAC ?? (object)DBNull.Value;
                                cmd.Parameters.Add("@IP", SqlDbType.NVarChar).Value = computadorIp;
                                cmd.Parameters.Add("@Processador", SqlDbType.NVarChar).Value = hardwareInfo.Processador?.Nome ?? (object)DBNull.Value;
                                cmd.Parameters.Add("@ProcessadorFabricante", SqlDbType.NVarChar).Value = hardwareInfo.Processador?.Fabricante ?? (object)DBNull.Value;
                                cmd.Parameters.Add("@ProcessadorCore", SqlDbType.NVarChar).Value = hardwareInfo.Processador?.Cores.ToString() ?? (object)DBNull.Value;
                                cmd.Parameters.Add("@ProcessadorThread", SqlDbType.NVarChar).Value = hardwareInfo.Processador?.Threads.ToString() ?? (object)DBNull.Value;
                                cmd.Parameters.Add("@ProcessadorClock", SqlDbType.NVarChar).Value = hardwareInfo.Processador?.ClockSpeed ?? (object)DBNull.Value;
                                cmd.Parameters.Add("@Ram", SqlDbType.NVarChar).Value = hardwareInfo.Ram?.RamTotal ?? (object)DBNull.Value;
                                cmd.Parameters.Add("@RamTipo", SqlDbType.NVarChar).Value = hardwareInfo.Ram?.Tipo ?? (object)DBNull.Value;
                                cmd.Parameters.Add("@RamVelocidade", SqlDbType.NVarChar).Value = hardwareInfo.Ram?.Velocidade ?? (object)DBNull.Value;
                                cmd.Parameters.Add("@RamVoltagem", SqlDbType.NVarChar).Value = hardwareInfo.Ram?.Voltagem ?? (object)DBNull.Value;
                                cmd.Parameters.Add("@RamPorModule", SqlDbType.NVarChar).Value = hardwareInfo.Ram?.PorModulo ?? (object)DBNull.Value;
                                cmd.Parameters.Add("@Usuario", SqlDbType.NVarChar).Value = hardwareInfo.Usuario?.Usuario ?? (object)DBNull.Value;
                                cmd.Parameters.Add("@Hostname", SqlDbType.NVarChar).Value = hardwareInfo.Usuario?.Hostname ?? (object)DBNull.Value;
                                cmd.Parameters.Add("@Fabricante", SqlDbType.NVarChar).Value = hardwareInfo.Fabricante ?? (object)DBNull.Value;
                                cmd.Parameters.Add("@SO", SqlDbType.NVarChar).Value = hardwareInfo.SO ?? (object)DBNull.Value;
                                cmd.Parameters.Add("@ArmazenamentoC", SqlDbType.NVarChar).Value = hardwareInfo.Armazenamento?.DriveC?.Letra ?? (object)DBNull.Value;
                                cmd.Parameters.Add("@ArmazenamentoCTotal", SqlDbType.NVarChar).Value = hardwareInfo.Armazenamento?.DriveC?.TotalGB ?? (object)DBNull.Value;
                                cmd.Parameters.Add("@ArmazenamentoCLivre", SqlDbType.NVarChar).Value = hardwareInfo.Armazenamento?.DriveC?.LivreGB ?? (object)DBNull.Value;
                                cmd.Parameters.Add("@ArmazenamentoD", SqlDbType.NVarChar).Value = hardwareInfo.Armazenamento?.DriveD?.Letra ?? (object)DBNull.Value;
                                cmd.Parameters.Add("@ArmazenamentoDTotal", SqlDbType.NVarChar).Value = hardwareInfo.Armazenamento?.DriveD?.TotalGB ?? (object)DBNull.Value;
                                cmd.Parameters.Add("@ArmazenamentoDLivre", SqlDbType.NVarChar).Value = hardwareInfo.Armazenamento?.DriveD?.LivreGB ?? (object)DBNull.Value;
                                cmd.Parameters.Add("@ConsumoCPU", SqlDbType.NVarChar).Value = hardwareInfo.ConsumoCPU ?? (object)DBNull.Value;
                                cmd.Parameters.Add("@DataColeta", SqlDbType.DateTime).Value = DateTime.Now;

                                cmd.ExecuteNonQuery();
                                Console.WriteLine($"Dados de {computadorIp} inseridos/atualizados com sucesso.");
                            }
                        }
                    }
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Erro de conexão com {computadorIp}: {ex.Message}");
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Erro ao deserializar JSON de {computadorIp}: {ex.Message}");
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"Erro de SQL para {computadorIp}: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro inesperado para {computadorIp}: {ex.Message}");
            }
        }
    }
}