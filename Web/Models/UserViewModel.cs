using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;


namespace Web.Models
{
    public class UserViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome é obrigatório.")]
        [Display(Name = "Nome Completo")]
        public string Nome { get; set; }

        [Required(ErrorMessage = "O login é obrigatório.")]
        public string Login { get; set; }

        [DataType(DataType.Password)]
        public string? Password { get; set; }

        [Required(ErrorMessage = "A função é obrigatória.")]
        [Display(Name = "Função")]
        public string Role { get; set; } // "Admin" or "Normal"

        [Display(Name = "Colaborador")]
        public string? ColaboradorCPF { get; set; }

        [Display(Name = "É Coordenador?")]
        public bool IsCoordinator { get; set; }

        public IEnumerable<SelectListItem>? Colaboradores { get; set; }
    }
}
