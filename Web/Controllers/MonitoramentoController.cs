using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Web.Models;
using Microsoft.AspNetCore.Authorization;
using System.Linq;

namespace Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class MonitoramentoController : Controller
    {
        private readonly string _connectionString;
        private readonly ILogger<MonitoramentoController> _logger;
        private readonly PingService _pingService;

        public MonitoramentoController(IConfiguration configuration, ILogger<MonitoramentoController> logger, System.Collections.Generic.IEnumerable<Microsoft.Extensions.Hosting.IHostedService> hostedServices)
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
                    var command = new SqlCommand("SELECT * FROM Rede ORDER BY IP", connection);
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
                                Observacao = reader["Observacao"].ToString(),
                                Status = reader["Status"]?.ToString()
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting network assets list for monitoring.");
                // Handle error appropriately
            }
            return View(redes);
        }

        [HttpGet]
        public IActionResult GetStatus()
        {
            var result = new List<object>();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var command = new SqlCommand("SELECT Id, IP, Status, PingHistory FROM Rede ORDER BY IP", connection);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var pingHistory = reader["PingHistory"] as string;
                            var lossPercentage = 0.0;
                            if (!string.IsNullOrEmpty(pingHistory))
                            {
                                var history = pingHistory.Split(',');
                                var failures = history.Count(s => s == "0");
                                if (history.Length > 0)
                                {
                                    lossPercentage = (double)failures / history.Length * 100;
                                }
                            }

                            result.Add(new
                            {
                                id = (int)reader["Id"],
                                status = reader["Status"]?.ToString(),
                                lossPercentage = lossPercentage
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting network assets status for monitoring.");
                // Handle error appropriately
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
