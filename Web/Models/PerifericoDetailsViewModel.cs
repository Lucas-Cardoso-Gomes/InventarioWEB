using System.Collections.Generic;

namespace Web.Models
{
    public class PerifericoDetailsViewModel
    {
        public Periferico Periferico { get; set; }
        public List<Manutencao> HistoricoManutencoes { get; set; }
        public List<HistoricoTroca> HistoricoTrocas { get; set; }
    }
}
