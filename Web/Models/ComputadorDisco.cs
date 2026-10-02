using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web.Models
{
    public class ComputadorDisco
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ComputadorMAC { get; set; }

        [Required]
        public string Letra { get; set; }

        public string? TotalGB { get; set; }

        public string? LivreGB { get; set; }

        [ForeignKey("ComputadorMAC")]
        public virtual Computador? Computador { get; set; }
    }
}
