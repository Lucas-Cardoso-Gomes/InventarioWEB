using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using Web.Models;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using Web.Services;
using Microsoft.Extensions.Hosting;
using System.Data;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class MonitoramentoController : Controller
    {
        private readonly IDatabaseService _databaseService;
        private readonly ILogger<MonitoramentoController> _logger;
        private readonly PingService _pingService;

        public MonitoramentoController(IDatabaseService databaseService, ILogger<MonitoramentoController> logger, IEnumerable<IHostedService> hostedServices)
        {
            _databaseService = databaseService;
            _logger = logger;
            _pingService = hostedServices.OfType<PingService>().FirstOrDefault();
        }

        public IActionResult Index(string tipo)
        {
            var redes = new List<Rede>();
            var tiposDeDispositivo = new List<string>();
            try
            {
                using (var connection = _databaseService.CreateConnection())
                {
                    connection.Open();

                    // Get all device types for the filter
                    using (var tipoCommand = connection.CreateCommand())
                    {
                        tipoCommand.CommandText = "SELECT DISTINCT Tipo FROM Rede ORDER BY Tipo";
                        using (var reader = tipoCommand.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                tiposDeDispositivo.Add(reader["Tipo"].ToString());
                            }
                        }
                    }

                    // Build the main query with an optional filter
                    var query = "SELECT Id, Tipo, IP, MAC, Nome, DataInclusao, DataAlteracao, Observacao FROM Rede";
                    if (!string.IsNullOrEmpty(tipo))
                    {
                        query += " WHERE Tipo = @Tipo";
                    }
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = query;
                        if (!string.IsNullOrEmpty(tipo))
                        {
                            var p = command.CreateParameter(); p.ParameterName = "@Tipo"; p.Value = tipo; command.Parameters.Add(p);
                        }

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                redes.Add(new Rede
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    Tipo = reader["Tipo"].ToString(),
                                    IP = reader["IP"].ToString(),
                                    MAC = reader["MAC"].ToString(),
                                    Nome = reader["Nome"].ToString(),
                                    DataInclusao = Convert.ToDateTime(reader["DataInclusao"]),
                                    DataAlteracao = reader["DataAlteracao"] != DBNull.Value ? Convert.ToDateTime(reader["DataAlteracao"]) : (DateTime?)null,
                                    Observacao = reader["Observacao"].ToString()
                                });
                            }
                        }
                    }
                }

                // Sort the list in-memory using System.Version for correct IP sorting
                // Note: IP parsing might fail if IPs are not strictly valid versions (e.g. empty or weird format)
                // Using a safer parse or TryParse would be better, but assuming valid IPs for now or handling exception
                 redes = redes.OrderBy<Rede, long>(r =>
                 {
                     if (System.Net.IPAddress.TryParse(r.IP, out var ip))
                     {
                         var bytes = ip.GetAddressBytes();
                         if (bytes.Length == 4)
                         {
                              return (long)BitConverter.ToUInt32(bytes.Reverse().ToArray(), 0);
                         }
                     }
                      return 0L;
                 }).ToList();

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
                using (var connection = _databaseService.CreateConnection())
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT Id, IP FROM Rede";
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                redes.Add(new Rede { Id = Convert.ToInt32(reader["Id"]), IP = reader["IP"].ToString() });
                            }
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
