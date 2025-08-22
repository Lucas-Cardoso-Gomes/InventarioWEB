using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web.Models
{
    public class AdaptadorRede
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ComputadorMAC { get; set; }

        public string Descricao { get; set; }
        public string EnderecoIP { get; set; }
        public string MascaraSubRede { get; set; }
        public string GatewayPadrao { get; set; }
        public string ServidoresDNS { get; set; }

        [ForeignKey("ComputadorMAC")]
        public virtual Computador Computador { get; set; }
    }
}
