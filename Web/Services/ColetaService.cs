using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.Sqlite;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Web.Models;
using Web.Services;
using System.IO;

namespace Web.Services
{
    public class ColetaService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ColetaService> _logger;
        private readonly LogService _logService;
        private readonly IDatabaseService _databaseService;
        private readonly string _solicitarInformacoes;
        private readonly string _encryptionKey;

        public ColetaService(IConfiguration configuration, ILogger<ColetaService> logger, LogService logService, IDatabaseService databaseService)
        {
            _configuration = configuration;
            _logger = logger;
            _logService = logService;
            _databaseService = databaseService;
            _solicitarInformacoes = _configuration.GetSection("Autenticacao")["SolicitarInformacoes"];
            _encryptionKey = _configuration.GetSection("Autenticacao")["EncryptionKey"];

            if (string.IsNullOrEmpty(_encryptionKey))
            {
                throw new Exception("EncryptionKey is missing in configuration.");
            }
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
                        await writer.WriteLineAsync(EncryptionHelper.Encrypt(_solicitarInformacoes, _encryptionKey));
                        onResult($"Solicitação enviada para: {computadorIp}");

                        // Use ReadLineAsync to avoid hanging
                        string encryptedResponse = await reader.ReadLineAsync();
                        string resposta;

                        try
                        {
                            resposta = EncryptionHelper.Decrypt(encryptedResponse.Trim(), _encryptionKey);
                        }
                        catch (Exception ex)
                        {
                            string msg = $"Erro ao descriptografar resposta de {computadorIp}: {ex.Message}";
                             _logService.AddLog("Error", msg, "Coleta");
                             onResult(msg);
                             return;
                        }

                        HardwareInfo hardwareInfo;
                        try
                        {
                            hardwareInfo = JsonSerializer.Deserialize<HardwareInfo>(resposta);
                        }
                        catch (JsonException jsonEx)
                        {
                            string message = $"Erro de JSON ao coletar dados de {computadorIp}: {jsonEx.Message}. Resposta recebida (Decrypted): '{resposta}'";
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
            using (var connection = _databaseService.CreateConnection())
            {
                connection.Open();
                // SQLite uses INSERT OR REPLACE (REPLACE INTO) or UPSERT syntax (INSERT ... ON CONFLICT DO UPDATE)
                // UPSERT is preferred for preserving data not in the new insert if needed, but here we update everything on match.
                // Or "INSERT OR REPLACE INTO" which replaces the whole row (deleting old one).
                // Let's use INSERT INTO ... ON CONFLICT(MAC) DO UPDATE SET ...

                string upsertQuery = @"
                    INSERT INTO Computadores (MAC, IP, Processador, ProcessadorFabricante, ProcessadorCore, ProcessadorThread, ProcessadorClock, Ram, RamTipo, RamVelocidade, RamVoltagem, RamPorModule, Hostname, Fabricante, SO, ArmazenamentoC, ArmazenamentoCTotal, ArmazenamentoCLivre, ArmazenamentoD, ArmazenamentoDTotal, ArmazenamentoDLivre, ConsumoCPU, DataColeta, PartNumber)
                    VALUES (@MAC, @IP, @Processador, @ProcessadorFabricante, @ProcessadorCore, @ProcessadorThread, @ProcessadorClock, @Ram, @RamTipo, @RamVelocidade, @RamVoltagem, @RamPorModule, @Hostname, @Fabricante, @SO, @ArmazenamentoC, @ArmazenamentoCTotal, @ArmazenamentoCLivre, @ArmazenamentoD, @ArmazenamentoDTotal, @ArmazenamentoDLivre, @ConsumoCPU, @DataColeta, @PartNumber)
                    ON CONFLICT(MAC) DO UPDATE SET
                        IP = excluded.IP,
                        Processador = excluded.Processador,
                        ProcessadorFabricante = excluded.ProcessadorFabricante,
                        ProcessadorCore = excluded.ProcessadorCore,
                        ProcessadorThread = excluded.ProcessadorThread,
                        ProcessadorClock = excluded.ProcessadorClock,
                        Ram = excluded.Ram,
                        RamTipo = excluded.RamTipo,
                        RamVelocidade = excluded.RamVelocidade,
                        RamVoltagem = excluded.RamVoltagem,
                        RamPorModule = excluded.RamPorModule,
                        Hostname = excluded.Hostname,
                        Fabricante = excluded.Fabricante,
                        SO = excluded.SO,
                        ArmazenamentoC = excluded.ArmazenamentoC,
                        ArmazenamentoCTotal = excluded.ArmazenamentoCTotal,
                        ArmazenamentoCLivre = excluded.ArmazenamentoCLivre,
                        ArmazenamentoD = excluded.ArmazenamentoD,
                        ArmazenamentoDTotal = excluded.ArmazenamentoDTotal,
                        ArmazenamentoDLivre = excluded.ArmazenamentoDLivre,
                        ConsumoCPU = excluded.ConsumoCPU,
                        DataColeta = excluded.DataColeta,
                        PartNumber = excluded.PartNumber;
                ";

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = upsertQuery;

                    var p1 = cmd.CreateParameter(); p1.ParameterName = "@MAC"; p1.Value = hardwareInfo.MAC ?? (object)DBNull.Value; cmd.Parameters.Add(p1);
                    var p2 = cmd.CreateParameter(); p2.ParameterName = "@IP"; p2.Value = computadorIp; cmd.Parameters.Add(p2);
                    var p3 = cmd.CreateParameter(); p3.ParameterName = "@Processador"; p3.Value = hardwareInfo.Processador?.Nome ?? (object)DBNull.Value; cmd.Parameters.Add(p3);
                    var p4 = cmd.CreateParameter(); p4.ParameterName = "@ProcessadorFabricante"; p4.Value = hardwareInfo.Processador?.Fabricante ?? (object)DBNull.Value; cmd.Parameters.Add(p4);
                    var p5 = cmd.CreateParameter(); p5.ParameterName = "@ProcessadorCore"; p5.Value = hardwareInfo.Processador?.Cores.ToString() ?? (object)DBNull.Value; cmd.Parameters.Add(p5);
                    var p6 = cmd.CreateParameter(); p6.ParameterName = "@ProcessadorThread"; p6.Value = hardwareInfo.Processador?.Threads.ToString() ?? (object)DBNull.Value; cmd.Parameters.Add(p6);
                    var p7 = cmd.CreateParameter(); p7.ParameterName = "@ProcessadorClock"; p7.Value = hardwareInfo.Processador?.ClockSpeed ?? (object)DBNull.Value; cmd.Parameters.Add(p7);
                    var p8 = cmd.CreateParameter(); p8.ParameterName = "@Ram"; p8.Value = hardwareInfo.Ram?.RamTotal ?? (object)DBNull.Value; cmd.Parameters.Add(p8);
                    var p9 = cmd.CreateParameter(); p9.ParameterName = "@RamTipo"; p9.Value = hardwareInfo.Ram?.Tipo ?? (object)DBNull.Value; cmd.Parameters.Add(p9);
                    var p10 = cmd.CreateParameter(); p10.ParameterName = "@RamVelocidade"; p10.Value = hardwareInfo.Ram?.Velocidade ?? (object)DBNull.Value; cmd.Parameters.Add(p10);
                    var p11 = cmd.CreateParameter(); p11.ParameterName = "@RamVoltagem"; p11.Value = hardwareInfo.Ram?.Voltagem ?? (object)DBNull.Value; cmd.Parameters.Add(p11);
                    var p12 = cmd.CreateParameter(); p12.ParameterName = "@RamPorModule"; p12.Value = hardwareInfo.Ram?.PorModulo ?? (object)DBNull.Value; cmd.Parameters.Add(p12);
                    var p13 = cmd.CreateParameter(); p13.ParameterName = "@Hostname"; p13.Value = hardwareInfo.Usuario?.Hostname ?? (object)DBNull.Value; cmd.Parameters.Add(p13);
                    var p14 = cmd.CreateParameter(); p14.ParameterName = "@Fabricante"; p14.Value = hardwareInfo.Fabricante ?? (object)DBNull.Value; cmd.Parameters.Add(p14);
                    var p15 = cmd.CreateParameter(); p15.ParameterName = "@SO"; p15.Value = hardwareInfo.SO ?? (object)DBNull.Value; cmd.Parameters.Add(p15);
                    var p16 = cmd.CreateParameter(); p16.ParameterName = "@ArmazenamentoC"; p16.Value = hardwareInfo.Armazenamento?.DriveC?.Letra ?? (object)DBNull.Value; cmd.Parameters.Add(p16);
                    var p17 = cmd.CreateParameter(); p17.ParameterName = "@ArmazenamentoCTotal"; p17.Value = hardwareInfo.Armazenamento?.DriveC?.TotalGB ?? (object)DBNull.Value; cmd.Parameters.Add(p17);
                    var p18 = cmd.CreateParameter(); p18.ParameterName = "@ArmazenamentoCLivre"; p18.Value = hardwareInfo.Armazenamento?.DriveC?.LivreGB ?? (object)DBNull.Value; cmd.Parameters.Add(p18);
                    var p19 = cmd.CreateParameter(); p19.ParameterName = "@ArmazenamentoD"; p19.Value = hardwareInfo.Armazenamento?.DriveD?.Letra ?? (object)DBNull.Value; cmd.Parameters.Add(p19);
                    var p20 = cmd.CreateParameter(); p20.ParameterName = "@ArmazenamentoDTotal"; p20.Value = hardwareInfo.Armazenamento?.DriveD?.TotalGB ?? (object)DBNull.Value; cmd.Parameters.Add(p20);
                    var p21 = cmd.CreateParameter(); p21.ParameterName = "@ArmazenamentoDLivre"; p21.Value = hardwareInfo.Armazenamento?.DriveD?.LivreGB ?? (object)DBNull.Value; cmd.Parameters.Add(p21);
                    var p22 = cmd.CreateParameter(); p22.ParameterName = "@ConsumoCPU"; p22.Value = hardwareInfo.ConsumoCPU ?? (object)DBNull.Value; cmd.Parameters.Add(p22);
                    var p23 = cmd.CreateParameter(); p23.ParameterName = "@DataColeta"; p23.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); cmd.Parameters.Add(p23);
                    var p24 = cmd.CreateParameter(); p24.ParameterName = "@PartNumber"; p24.Value = hardwareInfo.PartNumber ?? (object)DBNull.Value; cmd.Parameters.Add(p24);

                    cmd.ExecuteNonQuery();
                }
            }
        }

    }
}
