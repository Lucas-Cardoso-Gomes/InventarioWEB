using System.Collections.Generic;
using web.Models;

namespace web.Models
{
    public class LogViewModel
    {
        public List<Log> Logs { get; set; }
        public List<string> Levels { get; set; }
        public List<string> Sources { get; set; }
        
        public string CurrentLevel { get; set; }
        public string CurrentSource { get; set; }
        public string SearchString { get; set; }

        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }

        public int TotalPages => (int)System.Math.Ceiling((double)TotalCount / PageSize);
    }
}
