using System.Collections.Generic;

namespace Web.Models
{
    public class MonitorIndexViewModel
    {
        public List<Monitor> Monitores { get; set; }

        public string SearchString { get; set; }

        // Filter data sources
        public List<string> Marcas { get; set; }
        public List<string> Tamanhos { get; set; }
        public List<string> Modelos { get; set; }

        // Current filter selections
        public List<string> CurrentMarcas { get; set; } = new List<string>();
        public List<string> CurrentTamanhos { get; set; } = new List<string>();
        public List<string> CurrentModelos { get; set; } = new List<string>();

        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }

        public int TotalPages => (int)System.Math.Ceiling((double)TotalCount / PageSize);

        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}
