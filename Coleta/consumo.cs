using System;
using System.Diagnostics;
using System.Threading;

namespace coleta
{
    public class Consumo
    {
        public static string Uso()
        {
            PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");

            _ = cpuCounter.NextValue();
            Thread.Sleep(1000);

            float totalUsage = 0;
            int samples = 10;

            for (int i = 0; i < samples; i++)
            {
                totalUsage += cpuCounter.NextValue();
                Thread.Sleep(1000);
            }

            float averageUsage = totalUsage / samples;
            string Consumo = averageUsage.ToString("0.00");
            
            return Consumo;
        }
    }
}
