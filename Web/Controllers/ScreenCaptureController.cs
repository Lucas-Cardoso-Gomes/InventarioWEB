using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.IO;
using System;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ScreenCaptureController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ScreenCaptureController> _logger;

        public ScreenCaptureController(IConfiguration configuration, ILogger<ScreenCaptureController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string ip)
        {
            if (string.IsNullOrEmpty(ip))
            {
                return BadRequest("IP address cannot be null or empty.");
            }

            var screenshot = await GetScreenshot(ip);
            if (screenshot != null)
            {
                return File(screenshot, "image/png");
            }

            return NotFound("Failed to capture screenshot.");
        }

        private async Task<byte[]> GetScreenshot(string ip)
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
                        await writer.WriteLineAsync("take_screenshot");

                        var base64Image = await reader.ReadToEndAsync();
                        if (!string.IsNullOrEmpty(base64Image) && !base64Image.Contains("Error"))
                        {
                            return Convert.FromBase64String(base64Image);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get screenshot from IP: {IP}", ip);
            }
            return null;
        }
    }
}
