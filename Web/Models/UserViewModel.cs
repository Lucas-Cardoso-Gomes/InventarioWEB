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

        [Display(Name = "Colaborador (CPF)")]
        public string? ColaboradorCPF { get; set; }

        [Display(Name = "Coordenador")]
        public int? CoordenadorId { get; set; }

        public IEnumerable<SelectListItem> Coordenadores { get; set; }
        public IEnumerable<SelectListItem> Colaboradores { get; set; }
    }
}
