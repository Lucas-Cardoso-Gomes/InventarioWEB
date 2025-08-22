using System.ComponentModel.DataAnnotations;

namespace Web.Models
{
    public class UserViewModel
    {
        [Required(ErrorMessage = "O nome é obrigatório.")]
        [Display(Name = "Nome Completo")]
        public string Nome { get; set; }

        [Required(ErrorMessage = "O login é obrigatório.")]
        public string Login { get; set; }

        [Required(ErrorMessage = "A senha é obrigatória.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "A função é obrigatória.")]
        [Display(Name = "Função")]
        public string Role { get; set; } // "Admin" or "Normal"
    }
}
