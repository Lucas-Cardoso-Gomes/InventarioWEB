using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Web.Models;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using Web.Services;
using Microsoft.Extensions.Hosting;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class MonitoramentoController : Controller
    {
        private readonly string _connectionString;
        private readonly ILogger<MonitoramentoController> _logger;
        private readonly PingService _pingService;

        public MonitoramentoController(IConfiguration configuration, ILogger<MonitoramentoController> logger, IEnumerable<IHostedService> hostedServices)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _logger = logger;
            _pingService = hostedServices.OfType<PingService>().FirstOrDefault();
        }

        public IActionResult Index(string tipo)
        {
            var redes = new List<Rede>();
            var tiposDeDispositivo = new List<string>();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    // Get all device types for the filter
                    var tipoCommand = new SqlCommand("SELECT DISTINCT Tipo FROM Rede ORDER BY Tipo", connection);
                    using (var reader = tipoCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            tiposDeDispositivo.Add(reader["Tipo"].ToString());
                        }
                    }

                    // Build the main query with an optional filter
                    var query = "SELECT Id, Tipo, IP, MAC, Nome, DataInclusao, DataAlteracao, Observacao FROM Rede";
                    if (!string.IsNullOrEmpty(tipo))
                    {
                        query += " WHERE Tipo = @Tipo";
                    }
                    var command = new SqlCommand(query, connection);
                    if (!string.IsNullOrEmpty(tipo))
                    {
                        command.Parameters.AddWithValue("@Tipo", tipo);
                    }

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            redes.Add(new Rede
                            {
                                Id = (int)reader["Id"],
                                Tipo = reader["Tipo"].ToString(),
                                IP = reader["IP"].ToString(),
                                MAC = reader["MAC"].ToString(),
                                Nome = reader["Nome"].ToString(),
                                DataInclusao = (DateTime)reader["DataInclusao"],
                                DataAlteracao = reader["DataAlteracao"] as DateTime?,
                                Observacao = reader["Observacao"].ToString()
                            });
                        }
                    }
                }

                // Sort the list in-memory using System.Version for correct IP sorting
                redes = redes.OrderBy(r => Version.Parse(r.IP)).ToList();

                if (_pingService != null)
                {
                    var pingStatuses = _pingService.GetPingStatuses();
                    foreach (var rede in redes)
                    {
                        if (pingStatuses.TryGetValue(rede.IP, out var statusInfo) && statusInfo.History.Any())
                        {
                            rede.Status = statusInfo.Status;
                            rede.PingCount = statusInfo.History.Count;

                            var successfulPings = statusInfo.History.Where(p => p.Success).ToList();
                            var failures = statusInfo.History.Count - successfulPings.Count;
                            rede.LossPercentage = (double)failures / statusInfo.History.Count * 100;

                            if (successfulPings.Any())
                            {
                                rede.AverageLatency = successfulPings.Average(p => p.Latency);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting network assets list for monitoring.");
            }

            ViewBag.TiposDeDispositivo = tiposDeDispositivo;
            ViewBag.SelectedTipo = tipo;

            return View(redes);
        }

        [HttpGet]
        public IActionResult GetStatus()
        {
            if (_pingService == null)
            {
                return Json(new List<object>());
            }

            var result = new List<object>();
            try
            {
                var redes = new List<Rede>();
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var command = new SqlCommand("SELECT Id, IP FROM Rede", connection);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            redes.Add(new Rede { Id = (int)reader["Id"], IP = reader["IP"].ToString() });
                        }
                    }
                }

                var pingStatuses = _pingService.GetPingStatuses();
                foreach (var rede in redes)
                {
                    if (pingStatuses.TryGetValue(rede.IP, out var statusInfo))
                    {
                        var lossPercentage = 0.0;
                        var pingCount = 0;
                        var averageLatency = 0.0;

                        if (statusInfo.History.Any())
                        {
                            pingCount = statusInfo.History.Count;
                            var successfulPings = statusInfo.History.Where(p => p.Success).ToList();
                            var failures = pingCount - successfulPings.Count;
                            lossPercentage = (double)failures / pingCount * 100;

                            if (successfulPings.Any())
                            {
                                averageLatency = successfulPings.Average(p => p.Latency);
                            }
                        }

                        result.Add(new
                        {
                            id = rede.Id,
                            status = statusInfo.Status,
                            lossPercentage = lossPercentage,
                            pingCount = pingCount,
                            averageLatency = averageLatency
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting network assets status for monitoring.");
            }
            return Json(result);
        }

        [HttpGet]
        public IActionResult GetUptime()
        {
            if (_pingService == null)
            {
                return Json(new { uptime = "Not available" });
            }
            var uptime = DateTime.UtcNow - _pingService.StartTime;
            return Json(new { uptime = uptime.ToString(@"dd\.hh\:mm\:ss") });
        }
    }
}
