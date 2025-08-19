using System.Management;
using Coleta.Models;

namespace coleta
{
    public class Armazenamento
    {
        public static StorageInfo GetStorageInfo()
        {
            var storageInfo = new StorageInfo();

            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_LogicalDisk WHERE DriveType=3"))
            {
                foreach (var result in searcher.Get())
                {
                    string letra = result["Name"]?.ToString();
                    if (letra == "C:")
                    {
                        storageInfo.DriveC = new DiskInfo
                        {
                            Letra = letra,
                            TotalGB = $"{Convert.ToUInt64(result["Size"]) / (1024 * 1024 * 1024)} GB",
                            LivreGB = $"{Convert.ToUInt64(result["FreeSpace"]) / (1024 * 1024 * 1024)} GB"
                        };
                    }
                    else if (letra == "D:")
                    {
                        storageInfo.DriveD = new DiskInfo
                        {
                            Letra = letra,
                            TotalGB = $"{Convert.ToUInt64(result["Size"]) / (1024 * 1024 * 1024)} GB",
                            LivreGB = $"{Convert.ToUInt64(result["FreeSpace"]) / (1024 * 1024 * 1024)} GB"
                        };
                    }
                }
            }

            if (storageInfo.DriveC == null)
            {
                storageInfo.DriveC = new DiskInfo { Letra = "C:", TotalGB = "0 GB", LivreGB = "0 GB" };
            }

            if (storageInfo.DriveD == null)
            {
                storageInfo.DriveD = new DiskInfo { Letra = "D:", TotalGB = "0 GB", LivreGB = "0 GB" };
            }

            return storageInfo;
        }
    }
}
