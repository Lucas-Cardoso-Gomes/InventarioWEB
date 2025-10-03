using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Web.Models;

namespace Web.Hubs
{
    public class ChatHub : Hub
    {
        private readonly string _connectionString;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(IConfiguration configuration, ILogger<ChatHub> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
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
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var sql = @"INSERT INTO ChamadoConversas (ChamadoID, UsuarioCPF, Mensagem, DataCriacao)
                                VALUES (@ChamadoID, @UsuarioCPF, @Mensagem, @DataCriacao)";
                    using (var cmd = new SqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@ChamadoID", chamadoId);
                        cmd.Parameters.AddWithValue("@UsuarioCPF", userCpf);
                        cmd.Parameters.AddWithValue("@Mensagem", message);
                        cmd.Parameters.AddWithValue("@DataCriacao", timestamp);
                        await cmd.ExecuteNonQueryAsync();
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