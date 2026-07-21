using System.Collections.Generic;

namespace Web.Models
{
    public class SmartphoneDetailsViewModel
    {
        public Smartphone Smartphone { get; set; }
        public List<HistoricoTroca> HistoricoTrocas { get; set; }
    }
}
