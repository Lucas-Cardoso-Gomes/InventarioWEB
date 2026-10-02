using System;

namespace Web.Models
{
    public class FeedbackConversa
    {
        public int ID { get; set; }
        public int FeedbackID { get; set; }
        public string? UsuarioCPF { get; set; } // Pode ser nulo
        public string Remetente { get; set; } // Nome ou "Anônimo" ou "Admin"
        public string Mensagem { get; set; }
        public DateTime DataCriacao { get; set; }
    }
}
