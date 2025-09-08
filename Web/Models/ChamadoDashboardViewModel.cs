using System;
using System.Collections.Generic;

namespace Web.Models
{
    public class ChamadoDashboardViewModel
    {
        public int TotalChamados { get; set; }
        public List<ChartData> Top10Servicos { get; set; }
        public List<ChartData> TotalChamadosPorAdmin { get; set; }
        public List<ChartData> Top10Colaboradores { get; set; }

        public ChamadoDashboardViewModel()
        {
            Top10Servicos = new List<ChartData>();
            TotalChamadosPorAdmin = new List<ChartData>();
            Top10Colaboradores = new List<ChartData>();
        }
    }

    public class ChartData
    {
        public string Label { get; set; }
        public int Value { get; set; }
    }
}
