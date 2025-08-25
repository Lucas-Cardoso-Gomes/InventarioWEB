using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace web.Models
{
    public enum ExportType
    {
        [Display(Name = "Equipamentos por Colaborador")]
        EquipamentosPorColaborador,
        [Display(Name = "Computadores por Processador")]
        ComputadoresPorProcessador,
        [Display(Name = "Computadores por Tamanho do Monitor")]
        ComputadoresPorTamanhoMonitor
    }

    public class ExportarViewModel
    {
        [Display(Name = "Tipo de Exportação")]
        public ExportType ExportType { get; set; }

        [Display(Name = "Filtro")]
        public string FilterValue { get; set; }

        public List<string> Colaboradores { get; set; } = new List<string>();
    }
}
