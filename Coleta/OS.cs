using System.Management;

namespace coleta
{
    public class OS
    {
        public static string GetOSInfo()
        {
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem"))
            {
                var result = searcher.Get().Cast<ManagementBaseObject>().FirstOrDefault();
                if (result != null)
                {
                    string edition = result["Caption"].ToString();
                    string version = result["Version"].ToString();
                    return $"{edition} (Versão: {version})";
                }
            }
            return "Informação do sistema operacional não disponível";
        }
    }
}
