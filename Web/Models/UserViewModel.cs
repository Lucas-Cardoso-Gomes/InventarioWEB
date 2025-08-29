using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Web.Models
{
    public class UserViewModel
    {
        [Required(ErrorMessage = "O nome é obrigatório.")]
        [Display(Name = "Nome Completo")]
        public string Nome { get; set; }

        [Required(ErrorMessage = "O login é obrigatório.")]
        public string Login { get; set; }

        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "A função é obrigatória.")]
        [Display(Name = "Função")]
        public string Role { get; set; }

        [Display(Name = "CPF")]
        public string? CPF { get; set; }

        [EmailAddress(ErrorMessage = "E-mail inválido.")]
        public string? Email { get; set; }

        public string? SenhaEmail { get; set; }

        public string? Teams { get; set; }

        public string? SenhaTeams { get; set; }

        public string? EDespacho { get; set; }

        public string? SenhaEDespacho { get; set; }

        public string? Genius { get; set; }

        public string? SenhaGenius { get; set; }

        public string? Ibrooker { get; set; }

        public string? SenhaIbrooker { get; set; }

        public string? Adicional { get; set; }

        public string? SenhaAdicional { get; set; }

        public string? Setor { get; set; }

        public string? Smartphone { get; set; }

        public string? TelefoneFixo { get; set; }

        public string? Ramal { get; set; }

        public string? Alarme { get; set; }

        public string? Videoporteiro { get; set; }

        public string? Obs { get; set; }

        [Display(Name = "Coordenador")]
        public int? CoordenadorId { get; set; }

        public IEnumerable<SelectListItem>? Coordenadores { get; set; }
    }
}
