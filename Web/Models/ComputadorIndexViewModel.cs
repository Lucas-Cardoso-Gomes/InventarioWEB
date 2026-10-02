using System.Collections.Generic;

namespace Web.Models
{
    public class ComputadorIndexViewModel
    {
        public List<Computador> Computadores { get; set; } = new List<Computador>();

        public string CurrentSort { get; set; }
        public string SearchString { get; set; }

        // Filter data sources
        public List<string> Fabricantes { get; set; } = new List<string>();
        public List<string> SOs { get; set; } = new List<string>();
        public List<string> ProcessadorFabricantes { get; set; } = new List<string>();
        public List<string> RamTipos { get; set; } = new List<string>();
        public List<string> Processadores { get; set; } = new List<string>();
        public List<string> Rams { get; set; } = new List<string>();

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
