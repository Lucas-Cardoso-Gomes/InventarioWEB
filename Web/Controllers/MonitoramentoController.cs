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

        public IActionResult Index()
        {
            var redes = new List<Rede>();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var command = new SqlCommand("SELECT Id, Tipo, IP, MAC, Nome, DataInclusao, DataAlteracao, Observacao FROM Rede ORDER BY IP", connection);
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

                if (_pingService != null)
                {
                    var pingStatuses = _pingService.GetPingStatuses();
                    foreach (var rede in redes)
                    {
                        if (pingStatuses.TryGetValue(rede.IP, out var statusInfo))
                        {
                            rede.Status = statusInfo.Status;
                            if (statusInfo.History.Any())
                            {
                                var failures = statusInfo.History.Count(s => !s);
                                rede.LossPercentage = (double)failures / statusInfo.History.Count * 100;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting network assets list for monitoring.");
            }

            var groupedRedes = redes.GroupBy(r => r.Tipo)
                                    .ToDictionary(g => g.Key, g => g.ToList());

            return View(groupedRedes);
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
                        if (statusInfo.History.Any())
                        {
                            var failures = statusInfo.History.Count(s => !s);
                            lossPercentage = (double)failures / statusInfo.History.Count * 100;
                        }

                        result.Add(new
                        {
                            id = rede.Id,
                            status = statusInfo.Status,
                            lossPercentage = lossPercentage
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
