using System;

namespace Web.Models
{
    public class HistoricoTroca
    {
        public int Id { get; set; }
        public string EquipamentoId { get; set; }
        public string TipoEquipamento { get; set; }
        public string CampoAlterado { get; set; }
        public string ValorAntigo { get; set; }
        public string ValorNovo { get; set; }
        public string DataAlteracao { get; set; }
        public string Usuario { get; set; }
    }
}
