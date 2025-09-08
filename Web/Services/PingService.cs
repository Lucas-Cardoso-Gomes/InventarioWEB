using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Data.SqlClient;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Web.Models;
using System.Collections.Generic;
using System.Linq;

namespace Web.Services
{
    public class PingService : BackgroundService
    {
        private readonly ILogger<PingService> _logger;
        private readonly IConfiguration _configuration;

        private readonly int _numberOfPingsToStore;
        public DateTime StartTime { get; private set; }

        public PingService(ILogger<PingService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _numberOfPingsToStore = _configuration.GetValue<int>("Monitoring:NumberOfPings", 120);
            StartTime = DateTime.UtcNow;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("PingService running at: {time}", DateTimeOffset.Now);
                try
                {
                    var connectionString = _configuration.GetConnectionString("DefaultConnection");
                    var redes = new List<Rede>();

                    using (var connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        var command = new SqlCommand("SELECT Id, IP, LastPingStatus, PingHistory FROM Rede", connection);
                        using (var reader = await command.ExecuteReaderAsync(stoppingToken))
                        {
                            while (await reader.ReadAsync(stoppingToken))
                            {
                                redes.Add(new Rede
                                {
                                    Id = (int)reader["Id"],
                                    IP = reader["IP"].ToString(),
                                    LastPingStatus = reader["LastPingStatus"] as bool?,
                                    PingHistory = reader["PingHistory"] as string
                                });
                            }
                        }
                    }

                    foreach (var rede in redes)
                    {
                        var ping = new Ping();
                        var reply = await ping.SendPingAsync(rede.IP, 1000); // 1 second timeout

                        var currentPingStatus = reply.Status == IPStatus.Success;
                        var newStatus = "Gray"; // Default status

                        var lastPing = rede.LastPingStatus;

                        if (currentPingStatus && lastPing == true)
                        {
                            newStatus = "Green";
                        }
                        else if (currentPingStatus == false && lastPing == false)
                        {
                            newStatus = "Red";
                        }
                        else
                        {
                            newStatus = "Yellow";
                        }

                        var history = !string.IsNullOrEmpty(rede.PingHistory) ? rede.PingHistory.Split(',').ToList() : new List<string>();
                        history.Insert(0, currentPingStatus ? "1" : "0");
                        if (history.Count > _numberOfPingsToStore)
                        {
                            history = history.Take(_numberOfPingsToStore).ToList();
                        }
                        var newPingHistory = string.Join(",", history);

                        using (var connection = new SqlConnection(connectionString))
                        {
                            await connection.OpenAsync(stoppingToken);
                            var command = new SqlCommand("UPDATE Rede SET Status = @Status, PreviousPingStatus = LastPingStatus, LastPingStatus = @CurrentPingStatus, PingHistory = @PingHistory WHERE Id = @Id", connection);
                            command.Parameters.AddWithValue("@Status", newStatus);
                            command.Parameters.AddWithValue("@CurrentPingStatus", currentPingStatus);
                            command.Parameters.AddWithValue("@PingHistory", newPingHistory);
                            command.Parameters.AddWithValue("@Id", rede.Id);
                            await command.ExecuteNonQueryAsync(stoppingToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while pinging devices.");
                }

                await Task.Delay(30000, stoppingToken);
            }
        }
    }
}
