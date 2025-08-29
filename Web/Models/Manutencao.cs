using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace web.Models
{
    public class Manutencao
    {
        [Key]
        public int Id { get; set; }

        [Display(Name = "MAC do Computador")]
        public string? ComputadorMAC { get; set; }

        [ForeignKey("ComputadorMAC")]
        public Computador? Computador { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Data")]
        public DateTime Data { get; set; }

        [Required(ErrorMessage = "A descrição é obrigatória.")]
        [Display(Name = "Descrição")]
        public string Descricao { get; set; }

        [Display(Name = "Custo")]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? Custo { get; set; }
    }
}
