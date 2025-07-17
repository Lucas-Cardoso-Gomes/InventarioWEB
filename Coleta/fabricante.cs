using System;
using System.Management;

namespace coleta
{
    public class Fabricante
    {
        public static string GetManufacturer()
        {
            try
            {
                string manufacturer = string.Empty;
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT Manufacturer FROM Win32_ComputerSystem");
                
                foreach (ManagementObject obj in searcher.Get())
                {
                    manufacturer = obj["Manufacturer"]?.ToString() ?? "Fabricante não identificado";
                }
                //System.Console.WriteLine(manufacturer);
                return manufacturer;
            }
            catch (Exception ex)
            {
                return $"Erro ao coletar o fabricante: {ex.Message}";
            }
        }
    }
}