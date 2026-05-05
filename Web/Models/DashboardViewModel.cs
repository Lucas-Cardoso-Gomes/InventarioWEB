using System;
using System.Collections.Generic;

namespace Web.Models
{
    public class DashboardViewModel
    {
        public int TotalComputadores { get; set; }
        public int TotalMonitores { get; set; }
        public int TotalPerifericos { get; set; }
        public int TotalRedes { get; set; }
        public int TotalSmartphones { get; set; }

        public List<EquipamentoDashboardItem> Equipamentos { get; set; } = new List<EquipamentoDashboardItem>();

        public List<int> GarantiaPieData { get; set; } = new List<int>();
        public List<ChartData> GarantiaBarData { get; set; } = new List<ChartData>();
    }

    public class EquipamentoDashboardItem
    {
        public string TipoEquipamento { get; set; }
        public string Identificador { get; set; }
        public string ModeloOuNome { get; set; }
        public DateTime? DataGarantia { get; set; }
        public string Backup { get; set; } // Specific to Computador
        public DateTime? DataColeta { get; set; } // Specific to Computador
        public string ColaboradorNome { get; set; }
    }
}
