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

        [StringLength(14)]
        public string? CPF { get; set; }

        [EmailAddress(ErrorMessage = "E-mail inválido.")]
        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(100)]
        public string? SenhaEmail { get; set; }

        [StringLength(100)]
        public string? Teams { get; set; }

        [StringLength(100)]
        public string? SenhaTeams { get; set; }

        [StringLength(100)]
        public string? EDespacho { get; set; }

        [StringLength(100)]
        public string? SenhaEDespacho { get; set; }

        [StringLength(100)]
        public string? Genius { get; set; }

        [StringLength(100)]
        public string? SenhaGenius { get; set; }

        [StringLength(100)]
        public string? Ibrooker { get; set; }

        [StringLength(100)]
        public string? SenhaIbrooker { get; set; }

        [StringLength(100)]
        public string? Adicional { get; set; }

        [StringLength(100)]
        public string? SenhaAdicional { get; set; }

        [StringLength(100)]
        public string? Setor { get; set; }

        [StringLength(100)]
        public string? Smartphone { get; set; }

        [StringLength(100)]
        public string? TelefoneFixo { get; set; }

        [StringLength(100)]
        public string? Ramal { get; set; }

        [StringLength(100)]
        public string? Alarme { get; set; }

        [StringLength(100)]
        public string? Videoporteiro { get; set; }

        public string? Obs { get; set; }

        public DateTime? DataInclusao { get; set; }

        public DateTime? DataAlteracao { get; set; }

        // Self-referencing foreign key for Supervisor
        public int? SupervisorId { get; set; }
        [ForeignKey("SupervisorId")]
        public virtual User Supervisor { get; set; }
    }
}
