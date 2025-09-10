using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        public string Nome { get; set; }

        [Required]
        public string Login { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [Required]
        public string Role { get; set; } // "Admin", "Normal", "Coordenador" or "Diretoria"

        public bool IsCoordinator { get; set; }

        public string? ColaboradorCPF { get; set; }
        [ForeignKey("ColaboradorCPF")]
        public virtual Colaborador? Colaborador { get; set; }
    }
}
