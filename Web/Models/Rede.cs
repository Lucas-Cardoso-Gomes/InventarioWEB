using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web.Models
{
    public class Rede
    {
        public Rede()
        {
            Children = new HashSet<Rede>();
        }

        public int Id { get; set; }

        [Required(ErrorMessage = "O campo Tipo é obrigatório.")]
        public string Tipo { get; set; } // Roteador, Switch, AP

        [Required(ErrorMessage = "O campo IP é obrigatório.")]
        [Display(Name = "Endereço IP")]
        public string IP { get; set; }

        [Display(Name = "Endereço MAC")]
        public string? MAC { get; set; }

        [Required(ErrorMessage = "O campo Nome é obrigatório.")]
        public string Nome { get; set; }

        [Display(Name = "Data de Inclusão")]
        public DateTime DataInclusao { get; set; }

        [Display(Name = "Data de Alteração")]
        public DateTime? DataAlteracao { get; set; }

        [Display(Name = "Observação")]
        public string? Observacao { get; set; }

        // Hierarchy properties
        [Display(Name = "Dispositivo Pai")]
        public int? ParentId { get; set; }

        [ForeignKey("ParentId")]
        public virtual Rede Parent { get; set; }

        public virtual ICollection<Rede> Children { get; set; }

        // Monitoring properties
        [NotMapped]
        public string? Status { get; set; }
        [NotMapped]
        public double LossPercentage { get; set; }
    }
}
