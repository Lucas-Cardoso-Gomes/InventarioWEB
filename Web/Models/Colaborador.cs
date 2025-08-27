using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Web.Models;

namespace web.Models
{
    public class Colaborador
    {
        [Key]
        [Required(ErrorMessage = "O CPF é obrigatório.")]
        [StringLength(14)]
        public string CPF { get; set; }

        [Required(ErrorMessage = "O Nome é obrigatório.")]
        [StringLength(100)]
        public string Nome { get; set; }

        [EmailAddress(ErrorMessage = "E-mail inválido.")]
        [StringLength(100)]
        [ValidateNever]
        public string? Email { get; set; }
        
        [StringLength(100)]
        [ValidateNever]
        public string? SenhaEmail { get; set; }

        [StringLength(100)]
        [ValidateNever]
        public string? Teams { get; set; }

        [StringLength(100)]
        [ValidateNever]
        public string? SenhaTeams { get; set; }

        [StringLength(100)]
        [ValidateNever]
        public string? EDespacho { get; set; }

        [StringLength(100)]
        [ValidateNever]
        public string? SenhaEDespacho { get; set; }

        [StringLength(100)]
        [ValidateNever]
        public string? Genius { get; set; }

        [StringLength(100)]
        [ValidateNever]
        public string? SenhaGenius { get; set; }

        [StringLength(100)]
        [ValidateNever]
        public string? Ibrooker { get; set; }

        [StringLength(100)]
        [ValidateNever]
        public string? SenhaIbrooker { get; set; }

        [StringLength(100)]
        [ValidateNever]
        public string? Adicional { get; set; }

        [StringLength(100)]
        [ValidateNever]
        public string? SenhaAdicional { get; set; }

        [StringLength(100)]
        [ValidateNever]
        public string? Setor { get; set; }

        [StringLength(100)]
        [ValidateNever]
        public string? Smartphone { get; set; }

        [StringLength(100)]
        [ValidateNever]
        public string? TelefoneFixo { get; set; }

        [StringLength(100)]
        [ValidateNever]
        public string? Ramal { get; set; }

        [StringLength(100)]
        [ValidateNever]
        public string? Alarme { get; set; }

        [StringLength(100)]
        [ValidateNever]
        public string? Videoporteiro { get; set; }
        
        [ValidateNever]
        public string? Obs { get; set; }

        [ValidateNever]
        public DateTime? DataInclusao { get; set; }

        [ValidateNever]
        public DateTime? DataAlteracao { get; set; }

        [StringLength(50)]
        public string? Funcao { get; set; }
    }
}
