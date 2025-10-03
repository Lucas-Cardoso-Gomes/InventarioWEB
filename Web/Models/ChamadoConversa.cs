using System;

namespace Web.Models
{
    public class ChamadoConversa
    {
        public int ID { get; set; }
        public int ChamadoID { get; set; }
        public string UsuarioCPF { get; set; }
        public string Mensagem { get; set; }
        public DateTime DataCriacao { get; set; }
        public string UsuarioNome { get; set; } // To display in the chat view
    }
}