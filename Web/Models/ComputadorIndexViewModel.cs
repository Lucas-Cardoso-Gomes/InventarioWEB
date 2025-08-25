using System.Collections.Generic;

namespace web.Models
{
    public class ComputadorIndexViewModel
    {
        public List<Computador> Computadores { get; set; }

        public string CurrentSort { get; set; }
        public string SearchString { get; set; }

        // Filter data sources
        public List<string> Fabricantes { get; set; }
        public List<string> SOs { get; set; }
        public List<string> ProcessadorFabricantes { get; set; }
        public List<string> RamTipos { get; set; }
        public List<string> Processadores { get; set; }
        public List<string> Rams { get; set; }

        // Current filter selections
        public List<string> CurrentFabricantes { get; set; } = new List<string>();
        public List<string> CurrentSOs { get; set; } = new List<string>();
        public List<string> CurrentProcessadorFabricantes { get; set; } = new List<string>();
        public List<string> CurrentRamTipos { get; set; } = new List<string>();
        public List<string> CurrentProcessadores { get; set; } = new List<string>();
        public List<string> CurrentRams { get; set; } = new List<string>();

        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }

        public int TotalPages => (int)System.Math.Ceiling((double)TotalCount / PageSize);

        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}
