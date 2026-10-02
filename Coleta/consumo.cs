using System;
using System.Diagnostics;
using System.Threading;

namespace coleta
{
    public class Consumo
    {
        public static string Uso()
        {
            try
            {
                using (PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total"))
                {
                    _ = cpuCounter.NextValue();
                    Thread.Sleep(200);
                    float usage = cpuCounter.NextValue();
                    return usage.ToString("0.00");
                }
            }
            catch
            {
                return "0.00";
            }
        }
    }
}
