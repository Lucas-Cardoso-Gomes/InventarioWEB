using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Web.Hubs
{
    public class WebRtcHub : Hub
    {
        public async Task SendOffer(string targetConnectionId, string offer)
        {
            await Clients.Client(targetConnectionId).SendAsync("ReceiveOffer", Context.ConnectionId, offer);
        }

        public async Task SendAnswer(string targetConnectionId, string answer)
        {
            await Clients.Client(targetConnectionId).SendAsync("ReceiveAnswer", Context.ConnectionId, answer);
        }

        public async Task SendCandidate(string targetConnectionId, string candidate)
        {
            await Clients.Client(targetConnectionId).SendAsync("ReceiveCandidate", Context.ConnectionId, candidate);
        }

        public async Task Join(string ip)
        {
            // Associates the connection ID with the agent's IP
            Context.Items["ip"] = ip;
            await Groups.AddToGroupAsync(Context.ConnectionId, ip);
            // Notify the technician that the agent is ready
            await Clients.Group(ip).SendAsync("AgentReady", Context.ConnectionId);
        }

        public override async Task OnDisconnectedAsync(System.Exception exception)
        {
            if (Context.Items.TryGetValue("ip", out var ip) && ip is string ipString)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, ipString);
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
