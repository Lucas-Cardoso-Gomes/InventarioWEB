using System.Management;

namespace coleta
{
    public class Processador
    {
        public static string GetProcessorInfo()
        {
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor"))
            {
                var result = searcher.Get().Cast<ManagementBaseObject>().FirstOrDefault();
                if (result != null)
                {
                    string name = result["Name"].ToString();
                    int cores = Convert.ToInt32(result["NumberOfCores"]);
                    int threads = Convert.ToInt32(result["NumberOfLogicalProcessors"]);
                    string clockSpeed = result["MaxClockSpeed"].ToString();
                    string fabricante = result["Manufacturer"].ToString();

                    return $"{name}\n{fabricante}\n{cores}\n{threads}\n{clockSpeed}";
                }
            }
            return "Informação do processador não disponível";
        }
    }
}
