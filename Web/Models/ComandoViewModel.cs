using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Web.Models
{
    public class ComandoViewModel
    {
        [Display(Name = "Tipo de Envio")]
        public string TipoEnvio { get; set; } // "ip" or "range"

        [Display(Name = "Endereço IP / Hostname")]
        public string IpAddress { get; set; }

        [Display(Name = "Faixa de IP")]
        public string IpRange { get; set; }

        [Required(ErrorMessage = "O comando é obrigatório.")]
        [Display(Name = "Comando a ser executado")]
        public string Comando { get; set; }

        // To display results
        public bool ComandoIniciado { get; set; } = false;
        public List<string> Resultados { get; set; } = new List<string>();
    }
}
