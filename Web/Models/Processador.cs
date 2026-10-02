using System.ComponentModel.DataAnnotations;

namespace Web.Models
{
    public class Processador
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome do processador é obrigatório.")]
        public string Nome { get; set; }

        public string? Fabricante { get; set; }

        public string? Cores { get; set; }

        public string? Threads { get; set; }

        public string? Clock { get; set; }
    }
}
