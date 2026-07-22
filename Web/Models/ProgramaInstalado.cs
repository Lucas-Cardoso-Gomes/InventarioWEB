using System;

namespace Web.Models
{
    public class ProgramaInstalado
    {
        public int ID { get; set; }
        public string ComputadorMAC { get; set; }
        public string Nome { get; set; }
        public string Versao { get; set; }
        public string Desenvolvedor { get; set; }
        public string PacoteId { get; set; }
        public DateTime DataColeta { get; set; }
        public string Hostname { get; set; } // Propriedade de visualização
    }
}
