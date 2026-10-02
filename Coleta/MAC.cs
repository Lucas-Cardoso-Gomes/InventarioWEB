using System;
using System.Linq;
using System.Net.NetworkInformation;

namespace coleta
{
    public class MAC
    {
        public static string GetFormattedMacAddress()
        {
            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface networkInterface in networkInterfaces)
            {
                if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                    networkInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                {
                    if (networkInterface.OperationalStatus == OperationalStatus.Up)
                    {
                        PhysicalAddress macAddress = networkInterface.GetPhysicalAddress();
                        if (macAddress != null && macAddress.GetAddressBytes().Length == 6)
                        {
                            return FormatMacAddress(macAddress.GetAddressBytes());
                        }
                    }
                }
            }
            return "Endereço MAC não encontrado";
        }

        private static string FormatMacAddress(byte[] macBytes)
        {
            return string.Join(":", macBytes.Select(b => b.ToString("X2")));
        }
    }
}
