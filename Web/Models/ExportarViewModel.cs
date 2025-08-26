using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Web.Models
{
    public enum ExportMode
    {
        [Display(Name = "Por Dispositivo")]
        PorDispositivo,
        [Display(Name = "Por Usuário")]
        PorColaborador
    }

    public enum DeviceType
    {
        [Display(Name = "Computadores")]
        Computadores,
        [Display(Name = "Monitores")]
        Monitores,
        [Display(Name = "Periféricos")]
        Perifericos
    }

    public class ExportarViewModel
    {
        [Display(Name = "Modo de Exportação")]
        public ExportMode ExportMode { get; set; }

        [Display(Name = "Tipo de Dispositivo")]
        public DeviceType DeviceType { get; set; }

        [Display(Name = "Usuário")]
        public string UserName { get; set; }
        public List<string> Colaboradores { get; set; } = new List<string>();


        // --- Filters for Computadores ---
        public List<string> Fabricantes { get; set; }
        public List<string> SOs { get; set; }
        public List<string> ProcessadorFabricantes { get; set; }
        public List<string> RamTipos { get; set; }
        public List<string> Processadores { get; set; }
        public List<string> Rams { get; set; }

        public List<string> CurrentFabricantes { get; set; } = new List<string>();
        public List<string> CurrentSOs { get; set; } = new List<string>();
        public List<string> CurrentProcessadorFabricantes { get; set; } = new List<string>();
        public List<string> CurrentRamTipos { get; set; } = new List<string>();
        public List<string> CurrentProcessadores { get; set; } = new List<string>();
        public List<string> CurrentRams { get; set; } = new List<string>();

        // --- Filters for Monitores ---
        public List<string> Marcas { get; set; }
        public List<string> Tamanhos { get; set; }
        public List<string> Modelos { get; set; }

        public List<string> CurrentMarcas { get; set; } = new List<string>();
        public List<string> CurrentTamanhos { get; set; } = new List<string>();
        public List<string> CurrentModelos { get; set; } = new List<string>();

        // --- Filters for Perifericos ---
        public List<string> TiposPeriferico { get; set; }
        public List<string> CurrentTiposPeriferico { get; set; } = new List<string>();
    }
}
