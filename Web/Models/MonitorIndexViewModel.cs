using System.Collections.Generic;

using Web.Models;

namespace Web.Models
{
    public class MonitorIndexViewModel
    {
        public List<Monitor> Monitores { get; set; }

        public List<string> Marcas { get; set; }
        public List<string> Tamanhos { get; set; }
        public List<string> Modelos { get; set; }

        public List<string> CurrentMarcas { get; set; } = new List<string>();
        public List<string> CurrentTamanhos { get; set; } = new List<string>();
        public List<string> CurrentModelos { get; set; } = new List<string>();
    }
}
