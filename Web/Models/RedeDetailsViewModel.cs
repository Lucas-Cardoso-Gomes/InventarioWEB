using System.Collections.Generic;

namespace Web.Models
{
    public class RedeDetailsViewModel
    {
        public Rede Rede { get; set; }
        public List<HistoricoTroca> HistoricoTrocas { get; set; }
    }
}
