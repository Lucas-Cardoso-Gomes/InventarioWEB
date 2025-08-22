using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Web.Data;
using Web.Models;

namespace Web.Services
{
    public class LogService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LogService> _logger;

        public LogService(ApplicationDbContext context, ILogger<LogService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task AddLogAsync(string level, string message, string source)
        {
            try
            {
                var logEntry = new Log
                {
                    Timestamp = DateTime.Now,
                    Level = level,
                    Message = message,
                    Source = source
                };

                _context.Logs.Add(logEntry);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao gravar no banco de dados de log. Log original: Level={Level}, Source={Source}, Message={Message}", level, source, message);
            }
        }
    }
}
