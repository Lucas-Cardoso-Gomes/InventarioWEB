using System.Collections.Generic;
using System.Management;
using Coleta.Models;

namespace Coleta
{
    public class Rede
    {
        public static List<NetworkAdapterInfo> GetNetworkAdapterInfo()
        {
            var networkAdapters = new List<NetworkAdapterInfo>();
            try
            {
                // Filtra apenas por adaptadores que têm uma configuração de IP
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled = TRUE"))
                {
                    foreach (var result in searcher.Get())
                    {
                        var adapter = new NetworkAdapterInfo
                        {
                            Descricao = result["Description"]?.ToString(),
                        };

                        // Endereços IP (pode haver mais de um, pegamos o primeiro)
                        if (result["IPAddress"] is string[] ipAddresses && ipAddresses.Length > 0)
                        {
                            adapter.EnderecoIP = ipAddresses[0];
                        }

                        // Máscaras de sub-rede
                        if (result["IPSubnet"] is string[] ipSubnets && ipSubnets.Length > 0)
                        {
                            adapter.MascaraSubRede = ipSubnets[0];
                        }

                        // Gateway padrão
                        if (result["DefaultIPGateway"] is string[] gateways && gateways.Length > 0)
                        {
                            adapter.GatewayPadrao = gateways[0];
                        }

                        // Servidores DNS
                        if (result["DNSServerSearchOrder"] is string[] dnsServers && dnsServers.Length > 0)
                        {
                            adapter.ServidoresDNS = string.Join(", ", dnsServers);
                        }

                        networkAdapters.Add(adapter);
                    }
                }
            }
            catch (ManagementException ex)
            {
                System.Console.WriteLine($"Erro ao consultar WMI para Rede: {ex.Message}");
                // Adiciona um item de erro para indicar que a coleta falhou
                networkAdapters.Add(new NetworkAdapterInfo { Descricao = "Erro na Coleta de Rede" });
            }

            return networkAdapters;
        }
    }
}
