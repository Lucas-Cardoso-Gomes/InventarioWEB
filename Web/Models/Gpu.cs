using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web.Models
{
    public class Gpu
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ComputadorMAC { get; set; }

        public string Nome { get; set; }
        public string Fabricante { get; set; }
        public string RamDedicadaGB { get; set; }

        [ForeignKey("ComputadorMAC")]
        public virtual Computador Computador { get; set; }
    }
}
