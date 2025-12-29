using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using Microsoft.Data.Sqlite;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Web.Models;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;
using System.Data;

namespace Web.Services
{
    public class PingService : BackgroundService
    {
        private readonly ILogger<PingService> _logger;
        private readonly IConfiguration _configuration;
        private readonly int _numberOfPingsToStore;
        private readonly ConcurrentDictionary<string, PingStatusInfo> _pingStatuses = new ConcurrentDictionary<string, PingStatusInfo>();
        private readonly IDatabaseService _databaseService;

        public DateTime StartTime { get; private set; }

        public PingService(ILogger<PingService> logger, IConfiguration configuration, IDatabaseService databaseService)
        {
            _logger = logger;
            _configuration = configuration;
            _numberOfPingsToStore = _configuration.GetValue<int>("Monitoring:NumberOfPings", 240);
            _databaseService = databaseService;
            StartTime = DateTime.UtcNow;
        }

        public ConcurrentDictionary<string, PingStatusInfo> GetPingStatuses()
        {
            return _pingStatuses;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Initial load of devices to monitor
            await LoadRedesAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("PingService running at: {time}", DateTimeOffset.Now);
                try
                {
                    var ipsToPing = _pingStatuses.Keys.ToList();

                    foreach (var ip in ipsToPing)
                    {
                        var ping = new Ping();
                        // Ping send async might throw if address is invalid or not reachable in a way ping doesn't handle gracefully
                        try {
                            var reply = await ping.SendPingAsync(ip, 1000); // 1 second timeout

                            var currentPingStatus = reply.Status == IPStatus.Success;

                            _pingStatuses.AddOrUpdate(ip, new PingStatusInfo(), (key, existingStatus) =>
                            {
                                var lastPing = existingStatus.LastPingStatus;
                                var newStatus = "Gray"; // Default status

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

                                existingStatus.Status = newStatus;
                                existingStatus.LastPingStatus = currentPingStatus;

                                var pingResult = new PingResult { Success = currentPingStatus, Latency = reply.RoundtripTime };
                                existingStatus.History.Insert(0, pingResult);

                                if (existingStatus.History.Count > _numberOfPingsToStore)
                                {
                                    existingStatus.History = existingStatus.History.Take(_numberOfPingsToStore).ToList();
                                }
                                return existingStatus;
                            });
                        } catch (Exception pingEx) {
                             _logger.LogWarning(pingEx, "Failed to ping {IP}", ip);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while pinging devices.");
                }

                await Task.Delay(15000, stoppingToken);
            }
        }

        private async Task LoadRedesAsync(CancellationToken stoppingToken)
        {
            try
            {
                using (var connection = _databaseService.CreateConnection())
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT IP FROM Rede";
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var ip = reader["IP"].ToString();
                                if (!string.IsNullOrEmpty(ip))
                                {
                                    _pingStatuses.TryAdd(ip, new PingStatusInfo { Status = "Gray", LastPingStatus = null });
                                }
                            }
                        }
                    }
                }
                _logger.LogInformation("Loaded {Count} devices to monitor.", _pingStatuses.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load devices from database.");
            }
            await Task.CompletedTask;
        }
    }
}
