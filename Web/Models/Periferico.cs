using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace web.Models
{
    public class Periferico
    {
        [Key]
        public int ID { get; set; }

        [Display(Name = "Colaborador")]
        public string ColaboradorNome { get; set; }

        [ForeignKey("ColaboradorNome")]
        public virtual Colaborador Colaborador { get; set; }

        [Required(ErrorMessage = "O Tipo é obrigatório.")]
        [StringLength(50)]
        public string Tipo { get; set; }

        [Display(Name = "Data de Entrega")]
        [DataType(DataType.Date)]
        public DateTime? DataEntrega { get; set; }

        [StringLength(50)]
        public string PartNumber { get; set; }
    }
}
