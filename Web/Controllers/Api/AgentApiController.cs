using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Cryptography;
using System.Text;
using Web.Models;
using Web.Services;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.IO;

namespace Web.Controllers.Api
{
    [ApiController]
    [Route("api/agent")]
    public class AgentApiController : ControllerBase
    {
        private readonly ColetaService _coletaService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AgentApiController> _logger;
        private readonly LogService _logService;

        public AgentApiController(ColetaService coletaService, IConfiguration configuration, ILogger<AgentApiController> logger, LogService logService)
        {
            _coletaService = coletaService;
            _configuration = configuration;
            _logger = logger;
            _logService = logService;
        }

        [HttpPost("telemetry")]
        public async Task<IActionResult> ReceiveTelemetry()
        {
            try
            {
                // Verify authentication using Authorization header (Bearer token pattern)
                var authHeader = Request.Headers["Authorization"].ToString();
                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                {
                    _logger.LogWarning("Telemetry rejected: Missing or invalid Authorization header.");
                    return Unauthorized();
                }

                var providedHash = authHeader.Substring("Bearer ".Length).Trim();
                var expectedKey = _configuration["Autenticacao:SolicitarInformacoes"];
                
                // Use a simple hash check matching the agent logic, but static for the API
                // For a more secure approach, a rotating nonce could be used, but this matches the existing level.
                // We'll just hash the key itself with a static salt or expect the key itself as the bearer token.
                // To keep it simple and secure, let's expect the agent to send the raw key (or a hash of it)
                // For simplicity let's compare the hash of the expected key.
                string expectedHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(expectedKey)));

                if (providedHash != expectedHash)
                {
                    _logger.LogWarning("Telemetry rejected: Invalid token.");
                    return Unauthorized();
                }

                // Get IP
                string ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                
                // Allow mapping IPv4-mapped IPv6 addresses to raw IPv4
                if (HttpContext.Connection.RemoteIpAddress != null && HttpContext.Connection.RemoteIpAddress.IsIPv4MappedToIPv6)
                {
                    ip = HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
                }

                // Fallback for forwarded headers if behind proxy
                if (Request.Headers.ContainsKey("X-Forwarded-For"))
                {
                    ip = Request.Headers["X-Forwarded-For"].ToString().Split(',')[0].Trim();
                }

                if (string.IsNullOrEmpty(ip))
                {
                    ip = "Unknown";
                }

                // Read and parse JSON
                using (var reader = new StreamReader(Request.Body))
                {
                    var body = await reader.ReadToEndAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var hardwareInfo = JsonSerializer.Deserialize<HardwareInfo>(body, options);

                    if (hardwareInfo == null || string.IsNullOrEmpty(hardwareInfo.MAC))
                    {
                        return BadRequest("Invalid payload or missing MAC address.");
                    }

                    _coletaService.SalvarDados(hardwareInfo, ip);
                    _logService.AddLog("Info", $"Telemetry recebida e salva de {ip} (MAC: {hardwareInfo.MAC})", "Coleta");

                    return Ok();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error receiving telemetry.");
                return StatusCode(500, "Internal server error.");
            }
        }
    }
}
