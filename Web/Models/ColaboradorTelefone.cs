using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web.Models
{
    public class ColaboradorTelefone
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ColaboradorCPF { get; set; }

        [Required]
        public string Tipo { get; set; } // Smartphone, TelefoneFixo, Ramal

        [Required]
        public string Numero { get; set; }

        [ForeignKey("ColaboradorCPF")]
        public virtual Colaborador? Colaborador { get; set; }
    }
}
