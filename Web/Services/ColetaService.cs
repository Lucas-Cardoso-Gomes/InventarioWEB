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
using Web.Services;

namespace Web.Services
{
    public class ColetaService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ColetaService> _logger;
        private readonly LogService _logService;
        private readonly string _connectionString;
        private readonly string _solicitarInformacoes;

        public ColetaService(IConfiguration configuration, ILogger<ColetaService> logger, LogService logService)
        {
            _configuration = configuration;
            _logger = logger;
            _logService = logService;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
            _solicitarInformacoes = _configuration.GetSection("Autenticacao")["SolicitarInformacoes"];
        }

        public async Task ColetarDadosAsync(string computadorIp, Action<string> onResult)
        {
            int serverPort = 27275;
            _logService.AddLog("Info", $"Iniciando coleta de dados para o IP: {computadorIp}", "Coleta");

            try
            {
                using (var client = new TcpClient())
                {
                    var connectTask = client.ConnectAsync(computadorIp, serverPort);
                    if (await Task.WhenAny(connectTask, Task.Delay(5000)) != connectTask) // Timeout aumentado para 5s
                    {
                        string message = $"Timeout ao conectar com: {computadorIp}";
                        _logService.AddLog("Warning", message, "Coleta");
                        onResult(message);
                        return;
                    }

                    await connectTask; // Propagate exceptions
                    _logService.AddLog("Info", $"Conexão bem-sucedida com o IP: {computadorIp}", "Coleta");

                    using (NetworkStream stream = client.GetStream())
                    using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        await writer.WriteLineAsync(_solicitarInformacoes);
                        onResult($"Solicitação enviada para: {computadorIp}");

                        string resposta = await reader.ReadToEndAsync();

                        HardwareInfo hardwareInfo;
                        try
                        {
                            hardwareInfo = JsonSerializer.Deserialize<HardwareInfo>(resposta);
                        }
                        catch (JsonException jsonEx)
                        {
                            string message = $"Erro de JSON ao coletar dados de {computadorIp}: {jsonEx.Message}. Resposta recebida: '{resposta}'";
                            _logService.AddLog("Error", message, "Coleta");
                            onResult(message);
                            return;
                        }

                        if (hardwareInfo == null || hardwareInfo.MAC == null)
                        {
                            string message = $"Resposta JSON recebida, mas o MAC é nulo para o IP: {computadorIp}. Resposta: {resposta}";
                            _logService.AddLog("Error", message, "Coleta");
                            onResult(message);
                            return;
                        }

                        SalvarDados(hardwareInfo, computadorIp);
                        string successMessage = $"Dados de {computadorIp} (MAC: {hardwareInfo.MAC}) salvos com sucesso.";
                        _logService.AddLog("Info", successMessage, "Coleta");
                        onResult(successMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMessage = $"Erro ao coletar dados de {computadorIp}: {ex.Message}";
                _logger.LogError(ex, errorMessage);
                _logService.AddLog("Error", errorMessage, "Coleta");
                onResult(errorMessage);
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
                        UPDATE SET IP = @IP, Processador = @Processador, ProcessadorFabricante = @ProcessadorFabricante, ProcessadorCore = @ProcessadorCore, ProcessadorThread = @ProcessadorThread, ProcessadorClock = @ProcessadorClock, Ram = @Ram, RamTipo = @RamTipo, RamVelocidade = @RamVelocidade, RamVoltagem = @RamVoltagem, RamPorModule = @RamPorModule, Hostname = @Hostname, Fabricante = @Fabricante, SO = @SO, ArmazenamentoC = @ArmazenamentoC, ArmazenamentoCTotal = @ArmazenamentoCTotal, ArmazenamentoCLivre = @ArmazenamentoCLivre, ArmazenamentoD = @ArmazenamentoD, ArmazenamentoDTotal = @ArmazenamentoDTotal, ArmazenamentoDLivre = @ArmazenamentoDLivre, ConsumoCPU = @ConsumoCPU, DataColeta = @DataColeta
                    WHEN NOT MATCHED THEN
                        INSERT (MAC, IP, Processador, ProcessadorFabricante, ProcessadorCore, ProcessadorThread, ProcessadorClock, Ram, RamTipo, RamVelocidade, RamVoltagem, RamPorModule, Hostname, Fabricante, SO, ArmazenamentoC, ArmazenamentoCTotal, ArmazenamentoCLivre, ArmazenamentoD, ArmazenamentoDTotal, ArmazenamentoDLivre, ConsumoCPU, DataColeta)
                        VALUES (@MAC, @IP, @Processador, @ProcessadorFabricante, @ProcessadorCore, @ProcessadorThread, @ProcessadorClock, @Ram, @RamTipo, @RamVelocidade, @RamVoltagem, @RamPorModule, @Hostname, @Fabricante, @SO, @ArmazenamentoC, @ArmazenamentoCTotal, @ArmazenamentoCLivre, @ArmazenamentoD, @ArmazenamentoDTotal, @ArmazenamentoDLivre, @ConsumoCPU, @DataColeta);";

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

    }
}
