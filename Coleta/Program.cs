using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Coleta.Models;
using Microsoft.AspNetCore.SignalR.Client;
using SIPSorcery.Net;
using SIPSorceryMedia.Abstractions;
using SIPSorceryMedia.Windows;
using SIPSorceryMedia.Encoders;

namespace Coleta
{
    class Program
    {
        private const string SIGNALR_HUB_URL = "http://localhost/webRtcHub";
        private static HubConnection _hubConnection;
        private static RTCPeerConnection _peerConnection;
        private static FileStream _fileStream;
        private static string _fileName;

        static async Task Main()
        {
            Console.WriteLine("Iniciando Agente Coleta WebRTC...");

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(SIGNALR_HUB_URL)
                .Build();

            _hubConnection.On<string, Sdp>("ReceiveOffer", async (fromConnectionId, offer) =>
            {
                Console.WriteLine($"Oferta recebida de {fromConnectionId}");
                var offerSdp = new RTCSessionDescriptionInit { sdp = offer.sdp, type = RTCSdpType.offer };
                await OnOfferReceived(fromConnectionId, offerSdp);
            });

            _hubConnection.On<string, IceCandidate>("ReceiveCandidate", (fromConnectionId, candidate) =>
            {
                if (_peerConnection != null)
                {
                    _peerConnection.addIceCandidate(new RTCIceCandidateInit { candidate = candidate.candidate, sdpMid = candidate.sdpMid, sdpMLineIndex = (ushort)candidate.sdpMLineIndex });
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

            var videoSource = new WindowsVideoEndPoint(new Vp8VideoEncoder());
            var videoTrack = new MediaStreamTrack(videoSource.GetVideoSourceFormats());
            _peerConnection.addTrack(videoTrack);

            _peerConnection.ondatachannel += (dataChannel) =>
            {
                Console.WriteLine($"Canal de dados '{dataChannel.label}' aberto.");
                dataChannel.onmessage += (dc, protocol, data) =>
                {
                    if (protocol == DataChannelPayloadProtocols.dcpp_string)
                    {
                        var command = System.Text.Encoding.UTF8.GetString(data);
                        HandleRemoteControlCommand(command, dataChannel);
                    }
                    else if (protocol == DataChannelPayloadProtocols.dcpp_binary)
                    {
                        _fileStream?.Write(data, 0, data.Length);
                    }
                };
            };

            _peerConnection.onicecandidate += (candidate) =>
            {
                if (candidate != null)
                {
                     _hubConnection.SendAsync("SendCandidate", fromConnectionId, candidate);
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
                await _hubConnection.SendAsync("SendAnswer", fromConnectionId, answer);
            }
        }

        private static void HandleRemoteControlCommand(string command, RTCDataChannel dataChannel)
        {
            if (command.StartsWith("file_start"))
            {
                var parts = command.Split(',');
                _fileName = parts[1];
                var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                var filePath = Path.Combine(desktopPath, _fileName);
                _fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
                return;
            }

            if (command == "file_end")
            {
                _fileStream?.Close();
                _fileStream = null;
                return;
            }

            var commandParts = command.Split(' ');
            var commandType = commandParts[0];

            try
            {
                if (commandType == "mouse_event")
                {
                    var type = commandParts[1];
                    int x = int.Parse(commandParts[2]);
                    int y = int.Parse(commandParts[3]);
                    int deltaY = int.Parse(commandParts[4]);
                    RemoteControl.HandleMouseEvent(type, x, y, deltaY);
                }
                else if (commandType == "keyboard_event")
                {
                    var key = commandParts[1];
                    var state = commandParts[2];
                    var vkCode = KeyCodeConverter.GetVirtualKeyCode(key);
                    if (vkCode != 0)
                    {
                        RemoteControl.SendKeyEvent(vkCode, state == "up");
                    }
                }
                else if (commandType == "set_clipboard")
                {
                    var text = string.Join(" ", commandParts.Skip(1));
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
