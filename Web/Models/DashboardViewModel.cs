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

        // Battery Wear properties
        public int BatteryGoodCount { get; set; }
        public int BatteryWarningCount { get; set; }
        public int BatteryCriticalCount { get; set; }
        public double AverageBatteryWear { get; set; }
        public EquipamentoDashboardItem WorstBatteryComputer { get; set; }
        public List<EquipamentoDashboardItem> TopWorstBatteries { get; set; } = new List<EquipamentoDashboardItem>();

        // CPU Usage properties
        public int CpuGoodCount { get; set; }
        public int CpuWarningCount { get; set; }
        public int CpuCriticalCount { get; set; }
        public double AverageCpuUsage { get; set; }
        public EquipamentoDashboardItem WorstCpuComputer { get; set; }
        public List<EquipamentoDashboardItem> TopWorstCpus { get; set; } = new List<EquipamentoDashboardItem>();

        // Coleta Data properties
        public List<ChartData> ColetaBarData { get; set; } = new List<ChartData>();
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
        public double? BateriaWearLevel { get; set; } // Specific to Computador
        public double? CpuUsage { get; set; } // Specific to Computador
    }
}
