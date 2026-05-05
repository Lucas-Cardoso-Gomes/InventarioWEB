using System;
using System.Runtime.InteropServices;

namespace coleta
{
    public class Uptime
    {
        [DllImport("kernel32")]
        extern static UInt64 GetTickCount64();

        public static string GetUptime()
        {
            try
            {
                TimeSpan uptime = TimeSpan.FromMilliseconds(GetTickCount64());
                return string.Format("{0}d {1}h {2}m", uptime.Days, uptime.Hours, uptime.Minutes);
            }
            catch
            {
                return "N/A";
            }
        }
    }
}
