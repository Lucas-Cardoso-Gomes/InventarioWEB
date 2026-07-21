using System.Collections.Generic;

namespace Web.Models
{
    public class ComputadorDetailsViewModel
    {
        public Computador Computador { get; set; }
        public List<Manutencao> HistoricoManutencoes { get; set; }
        public List<HistoricoTroca> HistoricoTrocas { get; set; }
    }
}
