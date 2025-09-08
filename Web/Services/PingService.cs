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

namespace Web.Services
{
    public class PingService : BackgroundService
    {
        private readonly ILogger<PingService> _logger;
        private readonly IConfiguration _configuration;

        public PingService(ILogger<PingService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
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
                        var command = new SqlCommand("SELECT Id, IP, LastPingStatus FROM Rede", connection);
                        using (var reader = await command.ExecuteReaderAsync(stoppingToken))
                        {
                            while (await reader.ReadAsync(stoppingToken))
                            {
                                redes.Add(new Rede
                                {
                                    Id = (int)reader["Id"],
                                    IP = reader["IP"].ToString(),
                                    LastPingStatus = reader["LastPingStatus"] as bool?
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

                        using (var connection = new SqlConnection(connectionString))
                        {
                            await connection.OpenAsync(stoppingToken);
                            var command = new SqlCommand("UPDATE Rede SET Status = @Status, PreviousPingStatus = LastPingStatus, LastPingStatus = @CurrentPingStatus WHERE Id = @Id", connection);
                            command.Parameters.AddWithValue("@Status", newStatus);
                            command.Parameters.AddWithValue("@CurrentPingStatus", currentPingStatus);
                            command.Parameters.AddWithValue("@Id", rede.Id);
                            await command.ExecuteNonQueryAsync(stoppingToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while pinging devices.");
                }

                await Task.Delay(30000, stoppingToken); // 30 seconds
            }
        }
    }
}
