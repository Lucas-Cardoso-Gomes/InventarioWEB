using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Web.Models
{
    public class Periferico
    {
        [Key]
        [Required(ErrorMessage = "O Part Number é obrigatório.")]
        [StringLength(50)]
        public string PartNumber { get; set; }

        [Display(Name = "Colaborador")]
        public string? ColaboradorCPF { get; set; }

        [ForeignKey("ColaboradorCPF")]
        [ValidateNever]
        public virtual Colaborador? Colaborador { get; set; }

        [NotMapped]
        public string? ColaboradorNome { get; set; }

        [Required(ErrorMessage = "O Tipo é obrigatório.")]
        [StringLength(50)]
        public string Tipo { get; set; }

        [Display(Name = "Data de Entrega")]
        [DataType(DataType.Date)]
        [ValidateNever]
        public DateTime? DataEntrega { get; set; }
    }
}
