using System.Collections.Generic;

namespace Web.Models
{
    public class DatabaseLogViewModel
    {
        public List<DatabaseLog> Logs { get; set; }
        public string EntityTypeFilter { get; set; }
        public string ActionTypeFilter { get; set; }
    }
}
