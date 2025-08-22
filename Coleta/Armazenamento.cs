using System.Collections.Generic;
using System.Management;
using Coleta.Models;

namespace coleta
{
    public class Armazenamento
    {
        public static StorageInfo GetStorageInfo()
        {
            var storageInfo = new StorageInfo
            {
                Discos = new List<DiskInfo>()
            };

            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_LogicalDisk WHERE DriveType=3"))
                {
                    foreach (var result in searcher.Get())
                    {
                        var disk = new DiskInfo
                        {
                            Letra = result["Name"]?.ToString(),
                            TotalGB = $"{Convert.ToUInt64(result["Size"]) / (1024 * 1024 * 1024)}",
                            LivreGB = $"{Convert.ToUInt64(result["FreeSpace"]) / (1024 * 1024 * 1024)}"
                        };
                        storageInfo.Discos.Add(disk);
                    }
                }
            }
            catch (ManagementException ex)
            {
                // Log or handle the exception if WMI is not available
                System.Console.WriteLine($"Erro ao consultar WMI para armazenamento: {ex.Message}");
            }

            return storageInfo;
        }
    }
}
