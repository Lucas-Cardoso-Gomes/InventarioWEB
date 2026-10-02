using System.Management;
using Coleta.Models;

namespace coleta
{
    public class Processador
    {
        public static ProcessorInfo GetProcessorInfo()
        {
            ProcessorInfo info = null;
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor"))
            {
                var result = searcher.Get().Cast<ManagementBaseObject>().FirstOrDefault();
                if (result != null)
                {
                    info = new ProcessorInfo
                    {
                        Nome = result["Name"]?.ToString(),
                        Fabricante = result["Manufacturer"]?.ToString(),
                        Cores = Convert.ToInt32(result["NumberOfCores"]),
                        Threads = Convert.ToInt32(result["NumberOfLogicalProcessors"]),
                        ClockSpeed = result["MaxClockSpeed"]?.ToString()
                    };
                }
            }

            if (info != null)
            {
                bool tempSet = false;
                try
                {
                    using (var searcher = new ManagementObjectSearcher(@"root\WMI", "SELECT * FROM MSAcpi_ThermalZoneTemperature"))
                    {
                        var result = searcher.Get().Cast<ManagementBaseObject>().FirstOrDefault();
                        if (result != null && result["CurrentTemperature"] != null)
                        {
                            // MSAcpi_ThermalZoneTemperature is in tenths of degrees Kelvin
                            uint tempK = Convert.ToUInt32(result["CurrentTemperature"]);
                            double tempC = (tempK / 10.0) - 273.15;
                            info.Temperatura = $"{Math.Round(tempC, 1)} °C";
                            tempSet = true;
                        }
                    }
                }
                catch
                {
                    // WMI path might not be available or require admin rights
                }

                if (!tempSet)
                {
                    try
                    {
                        using (var searcher = new ManagementObjectSearcher(@"root\CIMV2", "SELECT * FROM Win32_PerfFormattedData_Counters_ThermalZoneInformation"))
                        {
                            var result = searcher.Get().Cast<ManagementBaseObject>().FirstOrDefault();
                            if (result != null && result["Temperature"] != null)
                            {
                                // Win32_PerfFormattedData_Counters_ThermalZoneInformation Temperature is in degrees Kelvin
                                uint tempK = Convert.ToUInt32(result["Temperature"]);
                                double tempC = tempK - 273.15;
                                info.Temperatura = $"{Math.Round(tempC, 1)} °C";
                                tempSet = true;
                            }
                        }
                    }
                    catch
                    {
                        // Fallback also failed
                    }
                }

                if (!tempSet)
                {
                    info.Temperatura = "N/A";
                }
            }

            return info;
        }
    }
}
