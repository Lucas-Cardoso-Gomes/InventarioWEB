using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace web.Models
{
    public class Periferico
    {
        [Key]
        [ValidateNever]
        public int ID { get; set; }

        [Display(Name = "Usuário")]
        public int? UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [ValidateNever]
        public string? ColaboradorNome { get; set; }

        [Required(ErrorMessage = "O Tipo é obrigatório.")]
        [StringLength(50)]
        public string Tipo { get; set; }

        [Display(Name = "Data de Entrega")]
        [DataType(DataType.Date)]
        [ValidateNever]
        public DateTime? DataEntrega { get; set; }

        [StringLength(50)]
        [ValidateNever]
        public string PartNumber { get; set; }
    }
}
