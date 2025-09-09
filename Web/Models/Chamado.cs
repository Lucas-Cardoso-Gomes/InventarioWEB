using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;


namespace Web.Models
{
    public class Chamado
    {
        [Key]
        public int ID { get; set; }

        [Display(Name = "Admin")]
        public string? AdminCPF { get; set; }

        [ForeignKey("AdminCPF")]
        [ValidateNever]
        public virtual Colaborador? Admin { get; set; }

        [Required(ErrorMessage = "O CPF do colaborador é obrigatório.")]
        [Display(Name = "Colaborador")]
        public string ColaboradorCPF { get; set; }

        [ForeignKey("ColaboradorCPF")]
        [ValidateNever]
        public virtual Colaborador Colaborador { get; set; }

        [Required(ErrorMessage = "A descrição é obrigatória.")]
        [StringLength(1000, ErrorMessage = "A descrição não pode ter mais de 1000 caracteres.")]
        public string Servico { get; set; }

        [Required(ErrorMessage = "A descrição é obrigatória.")]
        [StringLength(1000, ErrorMessage = "A descrição não pode ter mais de 1000 caracteres.")]
        public string Descricao { get; set; }

        [Display(Name = "Data de Alteração")]
        public DateTime? DataAlteracao { get; set; }

        [Display(Name = "Data de Criação")]
        public DateTime DataCriacao { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Aberto";

        [NotMapped]
        public string? AdminNome { get; set; }

        [NotMapped]
        public string? ColaboradorNome { get; set; }
    }
}
