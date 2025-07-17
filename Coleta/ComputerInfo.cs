using System;
using System.Net;

namespace coleta
{
    public class ComputerInfo
    {
        public static string GetComputerName()
        {
            try
            {
                string hostName = Dns.GetHostName();
                return hostName;
            }
            catch (Exception ex)
            {
                return $"Erro ao obter o nome do computador: {ex.Message}";
            }
        }
    }
}
