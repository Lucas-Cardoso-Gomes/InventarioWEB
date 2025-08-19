using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using web.Models;

namespace web.Services
{
    public class ColetaService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ColetaService> _logger;
        private readonly string _connectionString;
        private readonly string _solicitarInformacoes;
        private readonly string _realizarComandos;

        public ColetaService(IConfiguration configuration, ILogger<ColetaService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
            _solicitarInformacoes = _configuration.GetSection("Autenticacao")["SolicitarInformacoes"];
            _realizarComandos = _configuration.GetSection("Autenticacao")["RealizarComandos"];
        }

        public async Task ColetarDadosAsync(string computadorIp, Action<string> onResult)
        {
            int serverPort = 27275;

            try
            {
                using (var client = new TcpClient())
                {
                    var connectTask = client.ConnectAsync(computadorIp, serverPort);
                    if (await Task.WhenAny(connectTask, Task.Delay(2000)) != connectTask)
                    {
                        onResult($"Timeout ao conectar com: {computadorIp}");
                        return;
                    }

                    await connectTask; // Propagate exceptions

                    using (NetworkStream stream = client.GetStream())
                    {
                        string autenticacao = $"{_solicitarInformacoes}\n";
                        byte[] data = Encoding.UTF8.GetBytes(autenticacao);
                        await stream.WriteAsync(data, 0, data.Length);
                        onResult($"Solicitação enviada para: {computadorIp}");

                        byte[] buffer = new byte[8192];
                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                        string resposta = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                        var hardwareInfo = JsonSerializer.Deserialize<HardwareInfo>(resposta);
                        if (hardwareInfo == null || hardwareInfo.MAC == null)
                        {
                            onResult($"Falha ao deserializar a resposta ou MAC é nulo para o IP: {computadorIp}");
                            return;
                        }

                        SalvarDados(hardwareInfo, computadorIp);
                        onResult($"Dados de {computadorIp} salvos com sucesso.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao coletar dados de {computadorIp}");
                onResult($"Erro ao coletar dados de {computadorIp}: {ex.Message}");
            }
        }

        private void SalvarDados(HardwareInfo hardwareInfo, string computadorIp)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                string mergeQuery = @"
                    MERGE INTO Computadores AS target
                    USING (VALUES (@MAC)) AS source (MAC)
                    ON target.MAC = source.MAC
                    WHEN MATCHED THEN
                        UPDATE SET IP = @IP, Processador = @Processador, ProcessadorFabricante = @ProcessadorFabricante, ProcessadorCore = @ProcessadorCore, ProcessadorThread = @ProcessadorThread, ProcessadorClock = @ProcessadorClock, Ram = @Ram, RamTipo = @RamTipo, RamVelocidade = @RamVelocidade, RamVoltagem = @RamVoltagem, RamPorModule = @RamPorModule, Usuario = @Usuario, Hostname = @Hostname, Fabricante = @Fabricante, SO = @SO, ArmazenamentoC = @ArmazenamentoC, ArmazenamentoCTotal = @ArmazenamentoCTotal, ArmazenamentoCLivre = @ArmazenamentoCLivre, ArmazenamentoD = @ArmazenamentoD, ArmazenamentoDTotal = @ArmazenamentoDTotal, ArmazenamentoDLivre = @ArmazenamentoDLivre, ConsumoCPU = @ConsumoCPU, DataColeta = @DataColeta
                    WHEN NOT MATCHED THEN
                        INSERT (MAC, IP, Processador, ProcessadorFabricante, ProcessadorCore, ProcessadorThread, ProcessadorClock, Ram, RamTipo, RamVelocidade, RamVoltagem, RamPorModule, Usuario, Hostname, Fabricante, SO, ArmazenamentoC, ArmazenamentoCTotal, ArmazenamentoCLivre, ArmazenamentoD, ArmazenamentoDTotal, ArmazenamentoDLivre, ConsumoCPU, DataColeta)
                        VALUES (@MAC, @IP, @Processador, @ProcessadorFabricante, @ProcessadorCore, @ProcessadorThread, @ProcessadorClock, @Ram, @RamTipo, @RamVelocidade, @RamVoltagem, @RamPorModule, @Usuario, @Hostname, @Fabricante, @SO, @ArmazenamentoC, @ArmazenamentoCTotal, @ArmazenamentoCLivre, @ArmazenamentoD, @ArmazenamentoDTotal, @ArmazenamentoDLivre, @ConsumoCPU, @DataColeta);";

                using (var cmd = new SqlCommand(mergeQuery, connection))
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
                }
            }
        }

        public async Task<string> EnviarComandoAsync(string computadorIp, string comando)
        {
             int serverPort = 27275;
            try
            {
                using (var client = new TcpClient())
                {
                    var connectTask = client.ConnectAsync(computadorIp, serverPort);
                    if (await Task.WhenAny(connectTask, Task.Delay(2000)) != connectTask)
                    {
                        return $"Timeout ao conectar com: {computadorIp}";
                    }

                    await connectTask;

                    using (NetworkStream stream = client.GetStream())
                    {
                        string autenticacao = $"{_realizarComandos}\n{comando}";
                        byte[] data = Encoding.UTF8.GetBytes(autenticacao);
                        await stream.WriteAsync(data, 0, data.Length);

                        byte[] buffer = new byte[8192];
                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                        string resposta = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        return $"Resultado de '{comando}' em {computadorIp}: {resposta}";
                    }
                }
            }
            catch (Exception ex)
            {
                 _logger.LogError(ex, $"Erro ao enviar comando para {computadorIp}");
                return $"Erro ao enviar comando para {computadorIp}: {ex.Message}";
            }
        }
    }
}
