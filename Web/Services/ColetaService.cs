using System;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Web.Data;
// Este using é para o modelo de DTO que vem do agente de coleta
using Coleta.Models;

namespace Web.Services
{
    public class ColetaService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ColetaService> _logger;
        private readonly LogService _logService;
        private readonly ApplicationDbContext _context;
        private readonly string _solicitarInformacoes;

        public ColetaService(IConfiguration configuration, ILogger<ColetaService> logger, LogService logService, ApplicationDbContext context)
        {
            _configuration = configuration;
            _logger = logger;
            _logService = logService;
            _context = context;
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

                    using (NetworkStream stream = client.GetStream())
                    using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        await writer.WriteLineAsync(_solicitarInformacoes);
                        onResult($"Solicitação enviada para: {computadorIp}");

                        string resposta = await reader.ReadToEndAsync();

                        Coleta.Models.HardwareInfo hardwareInfo;
                        try
                        {
                            hardwareInfo = JsonSerializer.Deserialize<Coleta.Models.HardwareInfo>(resposta);
                        }
                        catch (JsonException jsonEx)
                        {
                            string message = $"Erro de JSON ao coletar dados de {computadorIp}: {jsonEx.Message}. Resposta: '{resposta}'";
                            _logService.AddLog("Error", message, "Coleta");
                            onResult(message);
                            return;
                        }

                        if (hardwareInfo == null || string.IsNullOrWhiteSpace(hardwareInfo.MAC))
                        {
                            string message = $"Resposta JSON inválida ou MAC nulo para o IP: {computadorIp}. Resposta: {resposta}";
                            _logService.AddLog("Error", message, "Coleta");
                            onResult(message);
                            return;
                        }

                        await SalvarDadosAsync(hardwareInfo, computadorIp);
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

        private async Task SalvarDadosAsync(Coleta.Models.HardwareInfo hardwareInfo, string computadorIp)
        {
            var computador = await _context.Computadores
                .Include(c => c.Discos)
                .Include(c => c.GPUs)
                .Include(c => c.AdaptadoresRede)
                .FirstOrDefaultAsync(c => c.MAC == hardwareInfo.MAC);

            if (computador == null)
            {
                computador = new Web.Models.Computador { MAC = hardwareInfo.MAC };
                _context.Computadores.Add(computador);
            }
            else
            {
                // Limpa coleções antigas para substituir com os novos dados
                computador.Discos.Clear();
                computador.GPUs.Clear();
                computador.AdaptadoresRede.Clear();
            }

            // Mapeia os dados do DTO para a entidade
            computador.IP = computadorIp;
            computador.Usuario = hardwareInfo.Usuario?.Usuario;
            computador.Hostname = hardwareInfo.Usuario?.Hostname;
            computador.Fabricante = hardwareInfo.Fabricante;
            computador.SO = hardwareInfo.SO;
            computador.DataColeta = DateTime.Now;
            computador.ConsumoCPU = hardwareInfo.ConsumoCPU;

            // Mapeia Processador
            if (hardwareInfo.Processador != null)
            {
                computador.ProcessadorNome = hardwareInfo.Processador.Nome;
                computador.ProcessadorFabricante = hardwareInfo.Processador.Fabricante;
                computador.ProcessadorCores = hardwareInfo.Processador.Cores;
                computador.ProcessadorThreads = hardwareInfo.Processador.Threads;
                computador.ProcessadorClock = hardwareInfo.Processador.ClockSpeed;
            }

            // Mapeia RAM
            if (hardwareInfo.Ram != null)
            {
                computador.RamTotal = hardwareInfo.Ram.RamTotal;
                computador.RamTipo = hardwareInfo.Ram.Tipo;
                computador.RamVelocidade = hardwareInfo.Ram.Velocidade;
                computador.RamVoltagem = hardwareInfo.Ram.Voltagem;
                computador.RamPorModulo = hardwareInfo.Ram.PorModulo;
            }

            // Mapeia Discos
            if (hardwareInfo.Armazenamento?.Discos != null)
            {
                foreach (var discoInfo in hardwareInfo.Armazenamento.Discos)
                {
                    computador.Discos.Add(new Web.Models.Disco
                    {
                        Letra = discoInfo.Letra,
                        TotalGB = discoInfo.TotalGB,
                        LivreGB = discoInfo.LivreGB
                    });
                }
            }

            // Mapeia GPU
            if (hardwareInfo.GPU != null)
            {
                computador.GPUs.Add(new Web.Models.Gpu
                {
                    Nome = hardwareInfo.GPU.Nome,
                    Fabricante = hardwareInfo.GPU.Fabricante,
                    RamDedicadaGB = hardwareInfo.GPU.RamDedicadaGB
                });
            }

            // Mapeia Adaptadores de Rede
            if (hardwareInfo.AdaptadoresRede != null)
            {
                foreach (var adapterInfo in hardwareInfo.AdaptadoresRede)
                {
                    computador.AdaptadoresRede.Add(new Web.Models.AdaptadorRede
                    {
                        Descricao = adapterInfo.Descricao,
                        EnderecoIP = adapterInfo.EnderecoIP,
                        MascaraSubRede = adapterInfo.MascaraSubRede,
                        GatewayPadrao = adapterInfo.GatewayPadrao,
                        ServidoresDNS = adapterInfo.ServidoresDNS
                    });
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
