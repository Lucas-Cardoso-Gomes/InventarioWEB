using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Web.Models
{
    public class ColetaViewModel
    {
        [Display(Name = "Tipo de Coleta")]
        public string TipoColeta { get; set; } // "ip" or "range"

        [Display(Name = "Endereço IP / Hostname")]
        public string IpAddress { get; set; }

        [Display(Name = "Faixa de IP")]
        public string IpRange { get; set; }

        // To display results
        public bool ColetaIniciada { get; set; } = false;
        public List<string> Resultados { get; set; } = new List<string>();
    }
}
