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
        public string Role { get; set; } // "Admin", "Coordenador", "Normal"

        public string? ColaboradorCPF { get; set; }

        // Self-referencing foreign key for Coordinator
        public int? CoordenadorId { get; set; }
        [ForeignKey("CoordenadorId")]
        public virtual User Coordenador { get; set; }
    }
}
