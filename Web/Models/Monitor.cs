using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace web.Models
{
    public class Monitor
    {
        [Key]
        [Required(ErrorMessage = "O Part Number é obrigatório.")]
        [StringLength(50)]
        public string PartNumber { get; set; }

        [Display(Name = "Usuário")]
        public int? UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [ValidateNever]
        public string? ColaboradorNome { get; set; }

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
