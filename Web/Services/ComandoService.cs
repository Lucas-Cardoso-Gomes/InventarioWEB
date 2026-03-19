using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Web.Services
{
    public class ComandoService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ComandoService> _logger;
        private readonly LogService _logService;
        private readonly string _realizarComandos;

        public ComandoService(IConfiguration configuration, ILogger<ComandoService> logger, LogService logService)
        {
            _configuration = configuration;
            _logger = logger;
            _logService = logService;
            _realizarComandos = _configuration.GetSection("Autenticacao")["RealizarComandos"];
        }

        public async Task<string> EnviarComandoAsync(string computadorIp, string comando)
        {
            int serverPort = 27275;
            _logService.AddLog("Info", $"Enviando comando '{comando}' para o IP: {computadorIp}", "Comandos");

            try
            {
                using (var client = new TcpClient())
                {
                    var connectTask = client.ConnectAsync(computadorIp, serverPort);
                    if (await Task.WhenAny(connectTask, Task.Delay(5000)) != connectTask)
                    {
                        string message = $"Timeout ao conectar com: {computadorIp} para enviar o comando.";
                        _logService.AddLog("Warning", message, "Comandos");
                        return message;
                    }

                    await connectTask;

                    using (NetworkStream stream = client.GetStream())
                    using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        await writer.WriteLineAsync(_realizarComandos);
                        await writer.WriteLineAsync(comando);

                        string resposta = await reader.ReadToEndAsync();
                        resposta = resposta.Trim();

                        string successMessage = $"Resultado de '{comando}' em {computadorIp}: {resposta}";
                        _logService.AddLog("Info", successMessage, "Comandos");
                        return successMessage;
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMessage = $"Erro ao enviar comando '{comando}' para {computadorIp}: {ex.Message}";
                _logger.LogError(ex, errorMessage);
                _logService.AddLog("Error", errorMessage, "Comandos");
                return errorMessage;
            }
        }
    }
}
