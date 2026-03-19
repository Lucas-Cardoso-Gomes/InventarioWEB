using System.Collections.Generic;

namespace Web.Models
{
    public class PingStatusInfo
    {
        public string Status { get; set; }
        public bool? LastPingStatus { get; set; }
        public List<PingResult> History { get; set; } = new List<PingResult>();
    }
}
