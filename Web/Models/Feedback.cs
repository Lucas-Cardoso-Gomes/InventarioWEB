using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web.Models
{
    public class Feedback
    {
        public int ID { get; set; }
        public string Protocolo { get; set; }

        [Required(ErrorMessage = "O assunto é obrigatório.")]
        [StringLength(200)]
        public string Assunto { get; set; }

        [Required(ErrorMessage = "A mensagem é obrigatória.")]
        public string Mensagem { get; set; }

        public DateTime DataCriacao { get; set; }

        public string? UsuarioCPF { get; set; } // Pode ser nulo para anônimos

        [NotMapped]
        public string? AutorNome { get; set; } // Nome do autor, se vinculado a um colaborador

        public string Status { get; set; } = "Aberto";

        public List<FeedbackConversa> Conversas { get; set; } = new List<FeedbackConversa>();
    }
}
