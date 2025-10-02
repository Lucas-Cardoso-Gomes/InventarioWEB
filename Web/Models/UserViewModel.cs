using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace Web.Models
{
    public class UserViewModel
    {
        public string Uid { get; set; }

        [Required(ErrorMessage = "O nome é obrigatório.")]
        [Display(Name = "Nome Completo")]
        public string Nome { get; set; }

        [Required(ErrorMessage = "O login (email) é obrigatório.")]
        [EmailAddress(ErrorMessage = "Formato de email inválido.")]
        public string Login { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Senha")]
        public string? Password { get; set; }

        [Required(ErrorMessage = "A função é obrigatória.")]
        [Display(Name = "Função")]
        public string Role { get; set; } // e.g., "Admin", "Coordenador"

        [Display(Name = "Colaborador Associado")]
        public string? ColaboradorCPF { get; set; }

        public IEnumerable<SelectListItem>? Colaboradores { get; set; }
    }
}