using System.Management;
using Coleta.Models;
using System.Linq;

namespace Coleta
{
    public class GPU
    {
        public static GpuInfo GetGpuInfo()
        {
            var gpuInfo = new GpuInfo();
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController"))
                {
                    // Pegar a primeira placa de vídeo, que geralmente é a principal
                    var firstGpu = searcher.Get().OfType<ManagementObject>().FirstOrDefault();

                    if (firstGpu != null)
                    {
                        gpuInfo.Nome = firstGpu["Name"]?.ToString();
                        gpuInfo.Fabricante = firstGpu["AdapterCompatibility"]?.ToString();

                        // AdapterRAM está em bytes, converter para GB.
                        // O valor pode ser nulo ou não ser um número válido.
                        if (firstGpu["AdapterRAM"] != null && ulong.TryParse(firstGpu["AdapterRAM"].ToString(), out ulong ramBytes))
                        {
                            gpuInfo.RamDedicadaGB = $"{ramBytes / (1024 * 1024 * 1024)}";
                        }
                        else
                        {
                            gpuInfo.RamDedicadaGB = "N/A";
                        }
                    }
                }
            }
            catch (ManagementException ex)
            {
                System.Console.WriteLine($"Erro ao consultar WMI para GPU: {ex.Message}");
                gpuInfo.Nome = "Erro na Coleta";
            }
            return gpuInfo;
        }
    }
}
