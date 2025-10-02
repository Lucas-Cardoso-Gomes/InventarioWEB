using System;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Web.Models;
using Google.Cloud.Firestore;

namespace Web.Services
{
    public class ColetaService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ColetaService> _logger;
        private readonly LogService _logService;
        private readonly FirestoreDb _firestoreDb;
        private readonly string _solicitarInformacoes;

        public ColetaService(IConfiguration configuration, ILogger<ColetaService> logger, LogService logService, FirestoreDb firestoreDb)
        {
            _configuration = configuration;
            _logger = logger;
            _logService = logService;
            _firestoreDb = firestoreDb;
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
                    if (await Task.WhenAny(connectTask, Task.Delay(5000)) != connectTask)
                    {
                        string message = $"Timeout ao conectar com: {computadorIp}";
                        _logService.AddLog("Warning", message, "Coleta");
                        onResult(message);
                        return;
                    }

                    await connectTask;
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

                        if (hardwareInfo == null || string.IsNullOrEmpty(hardwareInfo.MAC))
                        {
                            string message = $"Resposta JSON recebida, mas o MAC é nulo ou vazio para o IP: {computadorIp}. Resposta: {resposta}";
                            _logService.AddLog("Error", message, "Coleta");
                            onResult(message);
                            return;
                        }

                        await SalvarDadosAsync(hardwareInfo, computadorIp);
                        string successMessage = $"Dados de {computadorIp} (MAC: {hardwareInfo.MAC}) salvos com sucesso no Firestore.";
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

        private async Task SalvarDadosAsync(HardwareInfo hardwareInfo, string computadorIp)
        {
            var computador = new Computador
            {
                MAC = hardwareInfo.MAC,
                IP = computadorIp,
                Hostname = hardwareInfo.Usuario?.Hostname,
                Fabricante = hardwareInfo.Fabricante,
                Processador = hardwareInfo.Processador?.Nome,
                ProcessadorFabricante = hardwareInfo.Processador?.Fabricante,
                ProcessadorCore = hardwareInfo.Processador?.Cores.ToString(),
                ProcessadorThread = hardwareInfo.Processador?.Threads.ToString(),
                ProcessadorClock = hardwareInfo.Processador?.ClockSpeed,
                Ram = hardwareInfo.Ram?.RamTotal,
                RamTipo = hardwareInfo.Ram?.Tipo,
                RamVelocidade = hardwareInfo.Ram?.Velocidade,
                RamVoltagem = hardwareInfo.Ram?.Voltagem,
                RamPorModule = hardwareInfo.Ram?.PorModulo,
                ArmazenamentoC = hardwareInfo.Armazenamento?.DriveC?.Letra,
                ArmazenamentoCTotal = hardwareInfo.Armazenamento?.DriveC?.TotalGB,
                ArmazenamentoCLivre = hardwareInfo.Armazenamento?.DriveC?.LivreGB,
                ArmazenamentoD = hardwareInfo.Armazenamento?.DriveD?.Letra,
                ArmazenamentoDTotal = hardwareInfo.Armazenamento?.DriveD?.TotalGB,
                ArmazenamentoDLivre = hardwareInfo.Armazenamento?.DriveD?.LivreGB,
                ConsumoCPU = hardwareInfo.ConsumoCPU,
                SO = hardwareInfo.SO,
                DataColeta = DateTime.UtcNow,
                PartNumber = hardwareInfo.PartNumber
            };

            var docRef = _firestoreDb.Collection("computadores").Document(computador.MAC);
            await docRef.SetAsync(computador, SetOptions.MergeAll);
        }
    }
}