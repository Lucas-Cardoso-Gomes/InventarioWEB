using System;
using System.Management;

namespace coleta
{
    public class Bateria
    {
        public static string GetWearLevel()
        {
            try
            {
                double designCapacity = 0;
                double fullChargeCapacity = 0;
                uint cycleCount = 0;

                // Design Capacity
                using (var searcher = new ManagementObjectSearcher(@"root\wmi", 
                    "SELECT DesignedCapacity FROM BatteryStaticData"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        designCapacity = Convert.ToDouble(obj["DesignedCapacity"]);
                    }
                }

                // Full Charge Capacity
                using (var searcher = new ManagementObjectSearcher(@"root\wmi", 
                    "SELECT FullChargedCapacity FROM BatteryFullChargedCapacity"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        fullChargeCapacity = Convert.ToDouble(obj["FullChargedCapacity"]);
                    }
                }

                // Cycle Count (nem todos dispositivos suportam)
                using (var searcher = new ManagementObjectSearcher(@"root\wmi", 
                    "SELECT CycleCount FROM BatteryCycleCount"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        if (obj["CycleCount"] != null)
                            cycleCount = Convert.ToUInt32(obj["CycleCount"]);
                    }
                }

                if (designCapacity > 0 && fullChargeCapacity > 0)
                {
                    double wearLevel = 100.0 - ((fullChargeCapacity / designCapacity) * 100.0);

                    if (wearLevel < 0)
                        wearLevel = 0;

                    return $"Ciclo: {cycleCount} - {wearLevel:0}% Desgaste";
                }

                return "N/A";
            }
            catch
            {
                return "N/A";
            }
        }
    }
}