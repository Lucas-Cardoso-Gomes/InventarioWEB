using System.Collections.Generic;

namespace Web.Models
{
    public class ColaboradorDetailsViewModel
    {
        public Colaborador Colaborador { get; set; } = new();
        public List<Computador> Computadores { get; set; } = new();
        public List<Monitor> Monitores { get; set; } = new();
        public List<Periferico> Perifericos { get; set; } = new();
        public List<Smartphone> Smartphones { get; set; } = new();
    }
}
