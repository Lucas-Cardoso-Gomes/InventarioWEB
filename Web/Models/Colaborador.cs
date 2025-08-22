using System;
using System.ComponentModel.DataAnnotations;

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
        public string Email { get; set; }

        [StringLength(100)]
        public string SenhaEmail { get; set; }

        [StringLength(100)]
        public string Teams { get; set; }

        [StringLength(100)]
        public string SenhaTeams { get; set; }

        [StringLength(100)]
        public string EDespacho { get; set; }

        [StringLength(100)]
        public string SenhaEDespacho { get; set; }

        [StringLength(100)]
        public string Genius { get; set; }

        [StringLength(100)]
        public string SenhaGenius { get; set; }

        [StringLength(100)]
        public string Ibrooker { get; set; }

        [StringLength(100)]
        public string SenhaIbrooker { get; set; }

        [StringLength(100)]
        public string Adicional { get; set; }

        [StringLength(100)]
        public string SenhaAdicional { get; set; }

        [StringLength(100)]
        public string Setor { get; set; }

        [StringLength(100)]
        public string Ramal { get; set; }

        [StringLength(100)]
        public string Alarme { get; set; }

        [StringLength(100)]
        public string Videoporteiro { get; set; }

        public string Obs { get; set; }

        public DateTime? DataInclusao { get; set; }

        public DateTime? DataAlteracao { get; set; }
    }
}
