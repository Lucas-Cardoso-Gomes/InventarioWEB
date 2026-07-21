using System.Collections.Generic;

namespace Web.Models
{
    public class MonitorDetailsViewModel
    {
        public Monitor Monitor { get; set; }
        public List<Manutencao> HistoricoManutencoes { get; set; }
        public List<HistoricoTroca> HistoricoTrocas { get; set; }
    }
}
