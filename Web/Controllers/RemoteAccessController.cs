using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Sockets;
using System.Net.Security;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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
        private static readonly Regex InputLineRegex = new Regex(
            @"^(m|d|u) -?\d{1,6}( -?\d{1,6}){0,2}$|^(w|h) -?\d{1,6}$|^k [A-Za-z0-9]{1,24} (down|up)$|^r$",
            RegexOptions.Compiled);

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
            Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
            Response.Headers["X-Accel-Buffering"] = "no";

            try
            {
                var (tcpClient, sslStream, reader, writer) = await OpenAgentAsync(ip, HttpContext.RequestAborted);
                using (tcpClient)
                using (sslStream)
                using (reader)
                using (writer)
                {
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
            catch (Exception ex)
            {
                if (!(ex is OperationCanceledException))
                {
                    _logger.LogError(ex, "Error getting MJPEG stream from {IP}", ip);
                }
            }
        }

        [AcceptVerbs("GET", "CONNECT")]
        public async Task InputSocket(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip) || !HttpContext.WebSockets.IsWebSocketRequest)
            {
                Response.StatusCode = 400;
                return;
            }

            using var ws = await HttpContext.WebSockets.AcceptWebSocketAsync();
            var ct = HttpContext.RequestAborted;

            TcpClient? tcpClient = null;
            SslStream? sslStream = null;
            StreamReader? reader = null;
            StreamWriter? writer = null;

            try
            {
                (tcpClient, sslStream, reader, writer) = await OpenAgentAsync(ip, ct);
                writer.AutoFlush = false;

                await writer.WriteLineAsync("input_session");
                await writer.FlushAsync();

                using var ackCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                ackCts.CancelAfter(TimeSpan.FromSeconds(5));
                var ack = await reader.ReadLineAsync(ackCts.Token);

                if (!string.Equals(ack?.Trim(), "OK", StringComparison.Ordinal))
                {
                    if (ws.State == WebSocketState.Open)
                    {
                        await ws.CloseAsync(WebSocketCloseStatus.PolicyViolation, "agente desatualizado", CancellationToken.None);
                    }
                    return;
                }

                var buffer = new byte[4096];
                using var ms = new MemoryStream();

                while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
                {
                    var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        if (ws.State == WebSocketState.CloseReceived)
                        {
                            await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Closed", CancellationToken.None);
                        }
                        break;
                    }

                    if (result.MessageType != WebSocketMessageType.Text)
                    {
                        continue;
                    }

                    ms.Write(buffer, 0, result.Count);
                    if (!result.EndOfMessage)
                    {
                        if (ms.Length > 64 * 1024)
                        {
                            ms.SetLength(0);
                        }
                        continue;
                    }

                    var message = Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Length);
                    ms.SetLength(0);

                    var lines = message.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    var hasValidLine = false;
                    foreach (var rawLine in lines)
                    {
                        var line = rawLine.TrimEnd('\r');
                        if (InputLineRegex.IsMatch(line))
                        {
                            await writer.WriteLineAsync(line);
                            hasValidLine = true;
                        }
                    }

                    if (hasValidLine)
                    {
                        await writer.FlushAsync();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                if (!ct.IsCancellationRequested)
                {
                    _logger.LogWarning("Timeout connecting to agent input session for {IP}", ip);
                    if (ws.State == WebSocketState.Open)
                    {
                        try
                        {
                            await ws.CloseAsync(WebSocketCloseStatus.EndpointUnavailable, "tempo limite ao conectar no agente", CancellationToken.None);
                        }
                        catch
                        {
                            // Ignore close failures
                        }
                    }
                }
            }
            catch (WebSocketException ex)
            {
                _logger.LogWarning(ex, "WebSocket error in InputSocket for {IP}", ip);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in InputSocket for {IP}", ip);
                if (ws.State == WebSocketState.Open)
                {
                    try
                    {
                        await ws.CloseAsync(WebSocketCloseStatus.InternalServerError, "Agent connection error", CancellationToken.None);
                    }
                    catch
                    {
                        // Ignore close failures
                    }
                }
            }
            finally
            {
                writer?.Dispose();
                reader?.Dispose();
                sslStream?.Dispose();
                tcpClient?.Dispose();
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
        public async Task<IActionResult> OpenTaskManager(string ip)
        {
            if (string.IsNullOrEmpty(ip))
            {
                return BadRequest("IP address cannot be null or empty.");
            }

            var result = await SendCommandToAgent(ip, "send_ctrl_alt_del");
            if (result != null && result.StartsWith("Error:", StringComparison.Ordinal))
            {
                return StatusCode(502, result);
            }

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
                var (tcpClient, sslStream, reader, writer) = await OpenAgentAsync(ip, HttpContext.RequestAborted);
                using (tcpClient)
                using (sslStream)
                using (reader)
                using (writer)
                {
                    await writer.WriteLineAsync($"upload_file {file.FileName} {file.Length}");

                    using (var fileStream = file.OpenReadStream())
                    {
                        await fileStream.CopyToAsync(sslStream, HttpContext.RequestAborted);
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

        private async Task<(TcpClient tcpClient, SslStream sslStream, StreamReader reader, StreamWriter writer)> OpenAgentAsync(string ip, CancellationToken ct)
        {
            var tcpClient = new TcpClient { NoDelay = true };
            SslStream? sslStream = null;
            StreamReader? reader = null;
            StreamWriter? writer = null;

            try
            {
                using (var connectCts = CancellationTokenSource.CreateLinkedTokenSource(ct))
                {
                    connectCts.CancelAfter(TimeSpan.FromSeconds(5));
                    await tcpClient.ConnectAsync(ip, 27275, connectCts.Token);
                }

                var networkStream = tcpClient.GetStream();
                var expectedThumbprint = _configuration["Seguranca:AgenteThumbprint"];
                sslStream = new SslStream(
                    networkStream,
                    false,
                    (sender, cert, chain, errors) => ValidateServerCertificate(sender, cert, chain, errors, expectedThumbprint),
                    null);

                await sslStream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions { TargetHost = "ColetaAgent" }, ct);

                reader = new StreamReader(sslStream, Encoding.UTF8);
                writer = new StreamWriter(sslStream, new UTF8Encoding(false)) { AutoFlush = true };

                var authKey = _configuration["Autenticacao:RealizarComandos"];
                var nonce = await reader.ReadLineAsync(ct);
                var authHash = Convert.ToBase64String(HMACSHA256.HashData(Encoding.UTF8.GetBytes(authKey ?? string.Empty), Encoding.UTF8.GetBytes(nonce ?? string.Empty)));

                await writer.WriteLineAsync(authHash);
                return (tcpClient, sslStream, reader, writer);
            }
            catch
            {
                writer?.Dispose();
                reader?.Dispose();
                sslStream?.Dispose();
                tcpClient.Dispose();
                throw;
            }
        }

        private async Task<string> SendCommandToAgent(string ip, string command)
        {
            try
            {
                var (tcpClient, sslStream, reader, writer) = await OpenAgentAsync(ip, HttpContext.RequestAborted);
                using (tcpClient)
                using (sslStream)
                using (reader)
                using (writer)
                {
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send command '{Command}' to IP: {IP}", command, ip);
                return $"Error: {ex.Message}";
            }
        }

        private static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors, string expectedThumbprint)
        {
            if (string.IsNullOrEmpty(expectedThumbprint))
            {
                return true;
            }

            if (certificate != null && certificate is X509Certificate2 cert2)
            {
                return cert2.Thumbprint.Equals(expectedThumbprint.Replace(":", ""), StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }
    }
}
