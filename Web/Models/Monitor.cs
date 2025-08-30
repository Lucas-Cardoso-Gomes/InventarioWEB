using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Web.Models
{
    public class Monitor
    {
        [Key]
        [Required(ErrorMessage = "O Part Number é obrigatório.")]
        [StringLength(50)]
        public string PartNumber { get; set; }

        [Display(Name = "Colaborador")]
        [ValidateNever]
        public string? ColaboradorNome { get; set; }

        [ForeignKey("ColaboradorNome")]
        [ValidateNever]
        public virtual Colaborador? Colaborador { get; set; }

        [StringLength(50)]
        [ValidateNever]
        public string? Marca { get; set; }

        [StringLength(50)]
        [Required(ErrorMessage = "O Modelo é obrigatório.")]
        public string Modelo { get; set; }

        [StringLength(20)]
        [ValidateNever]
        public string Tamanho { get; set; }
    }
}
