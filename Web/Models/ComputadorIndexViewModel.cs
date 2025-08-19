using System.Collections.Generic;

namespace web.Models
{
    public class ComputadorIndexViewModel
    {
        public List<Computador> Computadores { get; set; }

        public string CurrentSort { get; set; }
        public string SearchString { get; set; }

        // For advanced filters
        public List<string> Fabricantes { get; set; }
        public List<string> SOs { get; set; }
        public string CurrentFabricante { get; set; }
        public string CurrentSO { get; set; }

        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }

        public int TotalPages => (int)System.Math.Ceiling((double)TotalCount / PageSize);

        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}
