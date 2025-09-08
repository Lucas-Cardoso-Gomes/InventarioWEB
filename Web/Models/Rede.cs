using System;
using System.ComponentModel.DataAnnotations;

namespace Web.Models
{
    public class Rede
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O campo Tipo é obrigatório.")]
        public string Tipo { get; set; } // Roteador, Switch, AP

        [Required(ErrorMessage = "O campo IP é obrigatório.")]
        [Display(Name = "Endereço IP")]
        public string IP { get; set; }

        [Display(Name = "Endereço MAC")]
        public string MAC { get; set; }

        [Required(ErrorMessage = "O campo Nome é obrigatório.")]
        public string Nome { get; set; }

        [Display(Name = "Descrição")]
        public string? Descricao { get; set; }

        [Display(Name = "Data de Inclusão")]
        public DateTime DataInclusao { get; set; }

        [Display(Name = "Data de Alteração")]
        public DateTime? DataAlteracao { get; set; }

        [Display(Name = "Observação")]
        public string? Observacao { get; set; }

        // Monitoring properties
        public string Status { get; set; }
        public bool? LastPingStatus { get; set; }
        public bool? PreviousPingStatus { get; set; }
    }
}
