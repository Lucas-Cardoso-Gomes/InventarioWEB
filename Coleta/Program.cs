using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using SIPSorcery.Net;
using SIPSorceryMedia.Windows;

namespace Coleta
{
    class Program
    {
        private const string SIGNALR_HUB_URL = "http://localhost/webRtcHub";
        private static HubConnection _hubConnection;
        private static RTCPeerConnection _peerConnection;

        static async Task Main()
        {
            Console.WriteLine("Iniciando Agente Coleta WebRTC...");

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(SIGNALR_HUB_URL)
                .Build();

            _hubConnection.On<string, string>("ReceiveOffer", async (fromConnectionId, offer) =>
            {
                Console.WriteLine($"Oferta recebida de {fromConnectionId}");
                var offerSdp = new RTCSessionDescriptionInit { sdp = offer, type = RTCSdpType.offer };
                await OnOfferReceived(fromConnectionId, offerSdp);
            });

            _hubConnection.On<string, string>("ReceiveCandidate", (fromConnectionId, candidate) =>
            {
                if (_peerConnection != null)
                {
                    _peerConnection.addIceCandidate(new RTCIceCandidateInit(candidate, 0, ""));
                }
            });

            try
            {
                await _hubConnection.StartAsync();
                await _hubConnection.SendAsync("Join", GetLocalIPAddress());
                Console.WriteLine("Agente conectado ao Hub de Sinalização. Aguardando conexões...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao conectar ao Hub: {ex.Message}");
                return;
            }

            await Task.Delay(-1);
        }

        private static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return "127.0.0.1";
        }

        private static async Task OnOfferReceived(string fromConnectionId, RTCSessionDescriptionInit offer)
        {
            _peerConnection = new RTCPeerConnection(null);

            var videoSource = new WindowsVideoEndPoint(new VideoEncoder());
            var videoTrack = new MediaStreamTrack(videoSource.GetVideoSourceFormats());
            _peerConnection.addTrack(videoTrack);

            _peerConnection.OnDataChannel += (dataChannel) =>
            {
                Console.WriteLine($"Canal de dados '{dataChannel.label}' aberto.");
                dataChannel.onmessage += (dc, _, data) =>
                {
                    var msg = System.Text.Encoding.UTF8.GetString(data);
                    HandleRemoteControlCommand(msg, dataChannel);
                };
            };

            _peerConnection.onicecandidate += (candidate) =>
            {
                if (candidate != null)
                {
                     _hubConnection.SendAsync("SendCandidate", fromConnectionId, candidate.candidate);
                }
            };

            _peerConnection.onconnectionstatechange += (state) =>
            {
                Console.WriteLine($"Estado da conexão: {state}");
            };

            var result = _peerConnection.setRemoteDescription(offer);
            if (result == SetDescriptionResultEnum.OK)
            {
                var answer = _peerConnection.createAnswer(null);
                await _peerConnection.setLocalDescription(answer);
                await _hubConnection.SendAsync("SendAnswer", fromConnectionId, answer.sdp);
            }
        }

        private static void HandleRemoteControlCommand(string command, RTCDataChannel dataChannel)
        {
            var parts = command.Split(' ');
            var commandType = parts[0];

            try
            {
                if (commandType == "mouse_event")
                {
                    var type = parts[1];
                    int x = int.Parse(parts[2]);
                    int y = int.Parse(parts[3]);
                    int deltaY = int.Parse(parts[4]);
                    RemoteControl.HandleMouseEvent(type, x, y, deltaY);
                }
                else if (commandType == "keyboard_event")
                {
                    var key = parts[1];
                    var state = parts[2];
                    var vkCode = KeyCodeConverter.GetVirtualKeyCode(key);
                    if (vkCode != 0)
                    {
                        RemoteControl.SendKeyEvent(vkCode, state == "up");
                    }
                }
                else if (commandType == "set_clipboard")
                {
                    var text = string.Join(" ", parts.Skip(1));
                    RemoteControl.SetClipboardText(text);
                }
                else if (commandType == "get_clipboard")
                {
                    var text = RemoteControl.GetClipboardText();
                    dataChannel.send($"clipboard_data {text}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao processar comando: {ex.Message}");
            }
        }
    }
}
