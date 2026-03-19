using System.Collections.Generic;

namespace Web.Models
{
    public class PersistentLogViewModel
    {
        public List<PersistentLog> Logs { get; set; }
        public string EntityTypeFilter { get; set; }
        public string ActionTypeFilter { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => (int)System.Math.Ceiling((double)TotalCount / PageSize);
    }
}
