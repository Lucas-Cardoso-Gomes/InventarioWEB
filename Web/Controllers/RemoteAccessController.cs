using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Web.Models;

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

                    using (var ms = new MemoryStream(imageBytes))
                    using (var image = System.Drawing.Image.FromStream(ms))
                    {
                        Response.Headers.Append("X-Original-Width", image.Width.ToString());
                        Response.Headers.Append("X-Original-Height", image.Height.ToString());
                    }

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
        public async Task<IActionResult> SendMouse(string ip, [FromBody] MouseInput input)
        {
            var command = $"mouse_event {input.Type} {input.X} {input.Y} {input.DeltaY}";
            await SendCommandToAgent(ip, command);
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> GetClipboard(string ip)
        {
            var text = await SendCommandToAgent(ip, "get_clipboard");
            return Content(text);
        }

        [HttpPost]
        public async Task<IActionResult> SendClipboard(string ip, [FromBody] string text)
        {
            var command = $"set_clipboard {text}";
            await SendCommandToAgent(ip, command);
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> SendKeyboard(string ip, string key, string state)
        {
            var command = $"keyboard_event {key} {state}";
            await SendCommandToAgent(ip, command);
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> SendCommand(string ip, string command)
        {
            await SendCommandToAgent(ip, command);
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> UploadFile(string ip, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("File not selected or empty.");
            }

            try
            {
                using (var tcpClient = new TcpClient())
                {
                    await tcpClient.ConnectAsync(ip, 27275);
                    using (var stream = tcpClient.GetStream())
                    using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
                    {
                        var authKey = _configuration["Autenticacao:RealizarComandos"];
                        await writer.WriteLineAsync(authKey);

                        await writer.WriteLineAsync($"upload_file {file.FileName} {file.Length}");

                        using (var fileStream = file.OpenReadStream())
                        {
                            await fileStream.CopyToAsync(stream);
                        }
                    }
                }
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload file to IP: {IP}", ip);
                return StatusCode(500, "Failed to upload file.");
            }
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

                        if (command == "take_screenshot")
                        {
                            // Lógica robusta para ler a imagem com tamanho prefixado
                            var sizeLine = await reader.ReadLineAsync();
                            if (int.TryParse(sizeLine, out int size))
                            {
                                var buffer = new char[size];
                                await reader.ReadBlockAsync(buffer, 0, size);
                                return new string(buffer);
                            }
                            return "Error: Invalid size received.";
                        }

                        return await reader.ReadLineAsync(); // Para outros comandos
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
