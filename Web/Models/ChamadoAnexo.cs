using System;

namespace Web.Models
{
    public class ChamadoAnexo
    {
        public int ID { get; set; }
        public int ChamadoID { get; set; }
        public string NomeArquivo { get; set; }
        public string CaminhoArquivo { get; set; }
        public DateTime DataUpload { get; set; }
    }
}