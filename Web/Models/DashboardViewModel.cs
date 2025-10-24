using System.Collections.Generic;

namespace Web.Models
{
    public class DashboardViewModel
    {
        public int TotalComputadores { get; set; }
        public int OpenChamados { get; set; }
        public IEnumerable<Manutencao> RecentManutencoes { get; set; }
    }
}
