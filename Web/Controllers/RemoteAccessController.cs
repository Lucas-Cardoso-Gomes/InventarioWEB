using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class RemoteAccessController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<RemoteAccessController> _logger;

        public RemoteAccessController(IConfiguration configuration, ILogger<RemoteAccessController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public IActionResult Index(string ip)
        {
            if (string.IsNullOrEmpty(ip))
            {
                return BadRequest("IP address cannot be null or empty.");
            }
            ViewBag.Ip = ip;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetScreenStream(string ip)
        {
            try
            {
                var response = await SendCommandToAgent(ip, "take_screenshot");
                if (response != null && !response.Contains("Error"))
                {
                    var imageBytes = Convert.FromBase64String(response);
                    return File(imageBytes, "image/png");
                }
                return NotFound("Failed to get screen frame.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting screen stream from {IP}", ip);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        public async Task<IActionResult> SendMouse(string ip, int x, int y, string type)
        {
            var command = $"mouse_event {x} {y} {type}";
            await SendCommandToAgent(ip, command);
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> SendKeyboard(string ip, string key)
        {
            // Simple sanitization
            if (string.IsNullOrEmpty(key) || key.Length > 1)
            {
                // Handle special keys if necessary in the future
                return BadRequest("Invalid key.");
            }
            var command = $"keyboard_event {key}";
            await SendCommandToAgent(ip, command);
            return Ok();
        }

        private async Task<string> SendCommandToAgent(string ip, string command)
        {
            try
            {
                using (var tcpClient = new TcpClient())
                {
                    await tcpClient.ConnectAsync(ip, 27275);
                    using (var stream = tcpClient.GetStream())
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
                    {
                        var authKey = _configuration["Autenticacao:RealizarComandos"];

                        await writer.WriteLineAsync(authKey);
                        await writer.WriteLineAsync(command);

                        return await reader.ReadToEndAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send command '{Command}' to IP: {IP}", command, ip);
                return $"Error: {ex.Message}";
            }
        }
    }
}
