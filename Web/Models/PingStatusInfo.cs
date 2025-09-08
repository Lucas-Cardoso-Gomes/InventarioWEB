using System.Collections.Generic;

namespace Web.Models
{
    public class PingStatusInfo
    {
        public string Status { get; set; }
        public bool? LastPingStatus { get; set; }
        public List<bool> History { get; set; } = new List<bool>();
    }
}
