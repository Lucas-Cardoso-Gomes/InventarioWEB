using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Web.Models;
using Web.Services;
using System.Data;

namespace Web.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IDatabaseService _databaseService;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(IDatabaseService databaseService, ILogger<ChatHub> logger)
        {
            _databaseService = databaseService;
            _logger = logger;
        }

        public async Task JoinGroup(string chamadoId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, chamadoId);
        }

        public async Task SendMessage(int chamadoId, string message)
        {
            var userCpf = Context.User.FindFirstValue("ColaboradorCPF");
            var userName = Context.User.Identity.Name;
            var timestamp = DateTime.Now;

            try
            {
                using (var connection = _databaseService.CreateConnection())
                {
                    connection.Open();
                    var sql = @"INSERT INTO ChamadoConversas (ChamadoID, UsuarioCPF, Mensagem, DataCriacao)
                                VALUES (@ChamadoID, @UsuarioCPF, @Mensagem, @DataCriacao)";
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = sql;
                        var p1 = cmd.CreateParameter(); p1.ParameterName = "@ChamadoID"; p1.Value = chamadoId; cmd.Parameters.Add(p1);
                        var p2 = cmd.CreateParameter(); p2.ParameterName = "@UsuarioCPF"; p2.Value = userCpf; cmd.Parameters.Add(p2);
                        var p3 = cmd.CreateParameter(); p3.ParameterName = "@Mensagem"; p3.Value = message; cmd.Parameters.Add(p3);
                        var p4 = cmd.CreateParameter(); p4.ParameterName = "@DataCriacao"; p4.Value = timestamp.ToString("yyyy-MM-dd HH:mm:ss"); cmd.Parameters.Add(p4);
                        cmd.ExecuteNonQuery();
                    }
                }

                await Clients.Group(chamadoId.ToString()).SendAsync("ReceiveMessage", userName, message, timestamp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao salvar ou enviar mensagem do chat.");
            }
        }
    }
}
