using System.Collections.Generic;

namespace Web.Models
{
    public class ChamadoDashboardViewModel
    {
        public int TotalChamados { get; set; }
        public int ChamadosAbertos { get; set; }
        public int ChamadosEmAndamento { get; set; }
        public int ChamadosFechados { get; set; }
        public List<ChartData> Top10Servicos { get; set; } = new List<ChartData>();
        public List<ChartData> PrioridadeServicos { get; set; } = new List<ChartData>();
        public List<ChartData> Top10Usuarios { get; set; } = new List<ChartData>();
        public List<ChartData> HorarioMedioAbertura { get; set; } = new List<ChartData>();
        public List<ChartData> TopDiasDaSemana { get; set; } = new List<ChartData>();
        public List<ChartData> FiliaisQueMaisAbremChamados { get; set; } = new List<ChartData>();
    }
}
