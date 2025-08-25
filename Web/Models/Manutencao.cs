using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace web.Models
{
    public class Manutencao
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "O MAC do computador é obrigatório.")]
        [Display(Name = "MAC do Computador")]
        public string ComputadorMAC { get; set; }

        [ForeignKey("ComputadorMAC")]
        public Computador Computador { get; set; }

        [Display(Name = "Data de Manutenção de Hardware")]
        [DataType(DataType.Date)]
        public DateTime? DataManutencaoHardware { get; set; }

        [Display(Name = "Data de Manutenção de Software")]
        [DataType(DataType.Date)]
        public DateTime? DataManutencaoSoftware { get; set; }

        [Display(Name = "Manutenção Externa")]
        public string ManutencaoExterna { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime Data { get; set; }
    }
}
