using System.Management;
using Coleta.Models;

namespace coleta
{
    public class Processador
    {
        public static ProcessorInfo GetProcessorInfo()
        {
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor"))
            {
                var result = searcher.Get().Cast<ManagementBaseObject>().FirstOrDefault();
                if (result != null)
                {
                    return new ProcessorInfo
                    {
                        Nome = result["Name"]?.ToString(),
                        Fabricante = result["Manufacturer"]?.ToString(),
                        Cores = Convert.ToInt32(result["NumberOfCores"]),
                        Threads = Convert.ToInt32(result["NumberOfLogicalProcessors"]),
                        ClockSpeed = result["MaxClockSpeed"]?.ToString()
                    };
                }
            }
            return null;
        }
    }
}
