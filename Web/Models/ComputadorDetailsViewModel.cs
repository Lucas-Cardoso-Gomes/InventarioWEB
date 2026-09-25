using System.Collections.Generic;

namespace Web.Models
{
    public class ComputadorDetailsViewModel
    {
        public Computador Computador { get; set; }
        public List<Manutencao> HistoricoManutencoes { get; set; } = new();
        public List<HistoricoTroca> HistoricoTrocas { get; set; } = new();
        public List<ProgramaInstalado> ProgramasInstalados { get; set; } = new();
    }
}
