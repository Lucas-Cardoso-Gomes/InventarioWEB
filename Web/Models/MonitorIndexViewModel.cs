using System.Collections.Generic;

namespace web.Models
{
    public class MonitorIndexViewModel
    {
        public List<Monitor> Monitores { get; set; }

        // Filter data sources
        public List<string> Marcas { get; set; }
        public List<string> Tamanhos { get; set; }
        public List<string> Modelos { get; set; }

        // Current filter selections
        public List<string> CurrentMarcas { get; set; } = new List<string>();
        public List<string> CurrentTamanhos { get; set; } = new List<string>();
        public List<string> CurrentModelos { get; set; } = new List<string>();
    }
}
