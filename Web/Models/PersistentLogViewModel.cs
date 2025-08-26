using System.Collections.Generic;

namespace Web.Models
{
    public class PersistentLogViewModel
    {
        public List<PersistentLog> Logs { get; set; }
        public string EntityTypeFilter { get; set; }
        public string ActionTypeFilter { get; set; }
    }
}
