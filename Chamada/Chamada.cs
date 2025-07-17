using System;
using System.Data;
using System.Data.SqlClient;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Configuration;

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
                using (TcpClient client = new TcpClient(computadorIp, serverPort))
                using (NetworkStream stream = client.GetStream())
                {
                    string autenticacao = Configuration["Autenticacao"];
                    byte[] data = Encoding.UTF8.GetBytes(autenticacao);

                    stream.Write(data, 0, data.Length);
                    Console.WriteLine($"Solicitação enviada ao computador: {computadorIp}");

                    byte[] buffer = new byte[5120];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    string resposta = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    Console.WriteLine($"Resposta do computador: {computadorIp}");

                    var dadosColetados = resposta.Split('\n');

                    string Processador = dadosColetados[0].Trim();
                    string ProcessadorFabricante = dadosColetados[1].Trim();
                    string ProcessadorCore = dadosColetados[2].Trim();
                    string ProcessadorThread = dadosColetados[3].Trim();
                    string ProcessadorClock = dadosColetados[4].Trim();
                    string Ram = dadosColetados[5].Trim();
                    string RamTipo = dadosColetados[6].Trim();
                    string RamVelocidade = dadosColetados[7].Trim();
                    string RamVoltagem = dadosColetados[8].Trim();
                    string RamPorModule = dadosColetados[9].Trim();
                    string Usuario = dadosColetados[10].Trim();
                    string Hostname = dadosColetados[11].Trim();
                    string Fabricante = dadosColetados[12].Trim();
                    string MAC = dadosColetados[13].Trim();
                    string SO = dadosColetados[14].Trim();
                    string ConsumoCPU = dadosColetados[15].Trim();
                    string ArmazenamentoC = dadosColetados[16].Trim();
                    string ArmazenamentoCTotal = dadosColetados[17].Trim();
                    string ArmazenamentoCLivre = dadosColetados[18].Trim();
                    string ArmazenamentoD = dadosColetados[19].Trim();
                    string ArmazenamentoDTotal = dadosColetados[20].Trim();
                    string ArmazenamentoDLivre = dadosColetados[21].Trim();
                    string Void = dadosColetados[22].Trim();
                    

                    //Console.WriteLine(resposta);

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
                        INSERT (MAC, IP, Processador, ProcessadorFabricante, ProcessadorCore, ProcessadorThread, ProcessadorClock, Ram, RamTipo, RamVelocidade, RamVoltagem, RamPorModule, Hostname, Fabricante, SO, ArmazenamentoC, ArmazenamentoCTotal, ArmazenamentoCLivre, ArmazenamentoD, ArmazenamentoDTotal, ArmazenamentoDLivre, ConsumoCPU, DataColeta)
                        VALUES (@MAC, @IP, @Processador, @ProcessadorFabricante, @ProcessadorCore, @ProcessadorThread, @ProcessadorClock, @Ram, @RamTipo, @RamVelocidade, @RamVoltagem, @RamPorModule, @Hostname, @Fabricante, @SO, @ArmazenamentoC, @ArmazenamentoCTotal, @ArmazenamentoCLivre, @ArmazenamentoD, @ArmazenamentoDTotal, @ArmazenamentoDLivre, @ConsumoCPU, @DataColeta);
                ";

                        using (SqlCommand cmd = new SqlCommand(mergeQuery, connection))
                        {
                            cmd.Parameters.Add("@MAC", SqlDbType.NVarChar).Value = MAC;
                            cmd.Parameters.Add("@IP", SqlDbType.NVarChar).Value = computadorIp;
                            cmd.Parameters.Add("@Processador", SqlDbType.NVarChar).Value = Processador;
                            cmd.Parameters.Add("@ProcessadorFabricante", SqlDbType.NVarChar).Value = ProcessadorFabricante;
                            cmd.Parameters.Add("@ProcessadorCore", SqlDbType.NVarChar).Value = ProcessadorCore;
                            cmd.Parameters.Add("@ProcessadorThread", SqlDbType.NVarChar).Value = ProcessadorThread;
                            cmd.Parameters.Add("@ProcessadorClock", SqlDbType.NVarChar).Value = ProcessadorClock;
                            cmd.Parameters.Add("@Ram", SqlDbType.NVarChar).Value = Ram;
                            cmd.Parameters.Add("@RamTipo", SqlDbType.NVarChar).Value = RamTipo;
                            cmd.Parameters.Add("@RamVelocidade", SqlDbType.NVarChar).Value = RamVelocidade;
                            cmd.Parameters.Add("@RamVoltagem", SqlDbType.NVarChar).Value = RamVoltagem;
                            cmd.Parameters.Add("@RamPorModule", SqlDbType.NVarChar).Value = RamPorModule;
                            cmd.Parameters.Add("@Usuario", SqlDbType.NVarChar).Value = Usuario;
                            cmd.Parameters.Add("@Hostname", SqlDbType.NVarChar).Value = Hostname;
                            cmd.Parameters.Add("@Fabricante", SqlDbType.NVarChar).Value = Fabricante;
                            cmd.Parameters.Add("@SO", SqlDbType.NVarChar).Value = SO;
                            cmd.Parameters.Add("@ArmazenamentoC", SqlDbType.NVarChar).Value = ArmazenamentoC;
                            cmd.Parameters.Add("@ArmazenamentoCTotal", SqlDbType.NVarChar).Value = ArmazenamentoCTotal;
                            cmd.Parameters.Add("@ArmazenamentoCLivre", SqlDbType.NVarChar).Value = ArmazenamentoCLivre;
                            cmd.Parameters.Add("@ArmazenamentoD", SqlDbType.NVarChar).Value = ArmazenamentoD;
                            cmd.Parameters.Add("@ArmazenamentoDTotal", SqlDbType.NVarChar).Value = ArmazenamentoDTotal;
                            cmd.Parameters.Add("@ArmazenamentoDLivre", SqlDbType.NVarChar).Value = ArmazenamentoDLivre;
                            cmd.Parameters.Add("@ConsumoCPU", SqlDbType.NVarChar).Value = ConsumoCPU;
                            cmd.Parameters.Add("@DataColeta", SqlDbType.DateTime).Value = DateTime.Now;

                            cmd.ExecuteNonQuery();
                            Console.WriteLine("Dados inseridos/atualizados com sucesso na tabela 'Computadores'");
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"Erro ao inserir/atualizar dados no banco: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro inesperado: {ex.Message}");
            }
        }
    }
}