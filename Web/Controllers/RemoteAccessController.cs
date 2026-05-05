using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
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

                    return File(imageBytes, "image/jpeg");
                }
                return NotFound("Failed to get screen frame.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting screen stream from {IP}", ip);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet]
        public async Task GetScreenMJPEGStream(string ip)
        {
            if (string.IsNullOrEmpty(ip))
            {
                Response.StatusCode = 400;
                return;
            }

            Response.Headers.Append("Content-Type", "multipart/x-mixed-replace; boundary=--frame");

            try
            {
                using (var tcpClient = new TcpClient())
                {
                    await tcpClient.ConnectAsync(ip, 27275);
                    using (var networkStream = tcpClient.GetStream())
                    {
                        var expectedThumbprint = _configuration["Seguranca:AgenteThumbprint"];
                        using (var sslStream = new SslStream(networkStream, false, (sender, cert, chain, errors) => ValidateServerCertificate(sender, cert, chain, errors, expectedThumbprint), null))
                        {
                            await sslStream.AuthenticateAsClientAsync("ColetaAgent");

                            using (var reader = new StreamReader(sslStream, Encoding.UTF8))
                            using (var writer = new StreamWriter(sslStream, Encoding.UTF8) { AutoFlush = true })
                            {
                                var authKey = _configuration["Autenticacao:RealizarComandos"];
                                var nonce = await reader.ReadLineAsync();
                                var authHash = Convert.ToBase64String(HMACSHA256.HashData(Encoding.UTF8.GetBytes(authKey), Encoding.UTF8.GetBytes(nonce ?? string.Empty)));

                                await writer.WriteLineAsync(authHash);
                                await writer.WriteLineAsync("take_screenshot_stream");
                                await writer.FlushAsync(); // Make sure the command is sent before reading raw

                                byte[] sizeBuffer = new byte[4];
                                while (!HttpContext.RequestAborted.IsCancellationRequested)
                                {
                                    int headerRead = 0;
                                    while (headerRead < 4)
                                    {
                                        int read = await sslStream.ReadAsync(sizeBuffer, headerRead, 4 - headerRead, HttpContext.RequestAborted);
                                        if (read == 0) break;
                                        headerRead += read;
                                    }
                                    if (headerRead < 4) break;

                                    int size = BitConverter.ToInt32(sizeBuffer, 0);

                                    byte[] imageBytes = new byte[size];
                                    int totalRead = 0;
                                    while (totalRead < size)
                                    {
                                        int read = await sslStream.ReadAsync(imageBytes, totalRead, size - totalRead, HttpContext.RequestAborted);
                                        if (read == 0) break;
                                        totalRead += read;
                                    }

                                    if (totalRead < size) break;

                                    if (HttpContext.RequestAborted.IsCancellationRequested) break;

                                    byte[] headerBytes = Encoding.UTF8.GetBytes($"--frame\r\nContent-Type: image/jpeg\r\nContent-Length: {size}\r\n\r\n");
                                    await Response.Body.WriteAsync(headerBytes, 0, headerBytes.Length, HttpContext.RequestAborted);
                                    await Response.Body.WriteAsync(imageBytes, 0, imageBytes.Length, HttpContext.RequestAborted);
                                    await Response.Body.WriteAsync(Encoding.UTF8.GetBytes("\r\n"), 0, 2, HttpContext.RequestAborted);
                                    await Response.Body.FlushAsync();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (!(ex is OperationCanceledException))
                {
                    _logger.LogError(ex, "Error getting MJPEG stream from {IP}", ip);
                }
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
                    using (var networkStream = tcpClient.GetStream())
                    {
                        var expectedThumbprint = _configuration["Seguranca:AgenteThumbprint"];
                        using (var sslStream = new SslStream(networkStream, false, (sender, cert, chain, errors) => ValidateServerCertificate(sender, cert, chain, errors, expectedThumbprint), null))
                        {
                            await sslStream.AuthenticateAsClientAsync("ColetaAgent");

                        using (var reader = new StreamReader(sslStream, Encoding.UTF8))
                        using (var writer = new StreamWriter(sslStream, Encoding.UTF8) { AutoFlush = true })
                        {
                            var authKey = _configuration["Autenticacao:RealizarComandos"];
                            var nonce = await reader.ReadLineAsync();
                            var authHash = Convert.ToBase64String(HMACSHA256.HashData(Encoding.UTF8.GetBytes(authKey), Encoding.UTF8.GetBytes(nonce ?? string.Empty)));

                            await writer.WriteLineAsync(authHash);

                            await writer.WriteLineAsync($"upload_file {file.FileName} {file.Length}");

                            using (var fileStream = file.OpenReadStream())
                            {
                                await fileStream.CopyToAsync(sslStream);
                            }
                        }
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
                    using (var networkStream = tcpClient.GetStream())
                    {
                        var expectedThumbprint = _configuration["Seguranca:AgenteThumbprint"];
                        using (var sslStream = new SslStream(networkStream, false, (sender, cert, chain, errors) => ValidateServerCertificate(sender, cert, chain, errors, expectedThumbprint), null))
                        {
                            await sslStream.AuthenticateAsClientAsync("ColetaAgent");

                        using (var reader = new StreamReader(sslStream, Encoding.UTF8))
                        using (var writer = new StreamWriter(sslStream, Encoding.UTF8) { AutoFlush = true })
                        {
                            var authKey = _configuration["Autenticacao:RealizarComandos"];
                            var nonce = await reader.ReadLineAsync();
                            var authHash = Convert.ToBase64String(HMACSHA256.HashData(Encoding.UTF8.GetBytes(authKey), Encoding.UTF8.GetBytes(nonce ?? string.Empty)));

                            await writer.WriteLineAsync(authHash);
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
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send command '{Command}' to IP: {IP}", command, ip);
                return $"Error: {ex.Message}";
            }
        }

        private static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors, string expectedThumbprint)
        {
            // Allow bypassing if thumbprint is not configured (backward compatibility)
            if (string.IsNullOrEmpty(expectedThumbprint))
            {
                return true;
            }

            // Pinned certificate generated by the static RSA key in ColetaAgent
            if (certificate != null && certificate is X509Certificate2 cert2)
            {
                return cert2.Thumbprint.Equals(expectedThumbprint.Replace(":", ""), StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }
    }
}
