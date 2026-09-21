using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web.Models
{
    public class ColaboradorCredencial
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ColaboradorCPF { get; set; }

        [Required]
        public string Sistema { get; set; }

        public string? UsuarioSistema { get; set; }

        public string? SenhaSistema { get; set; }

        [ForeignKey("ColaboradorCPF")]
        public virtual Colaborador? Colaborador { get; set; }
    }
}
