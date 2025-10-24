using System.Collections.Generic;

namespace Web.Models
{
    public class UserIndexViewModel
    {
        public List<User> Users { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public string SearchString { get; set; }
        public string CurrentSort { get; set; }
        public List<string> CurrentRoles { get; set; }
        public List<string> Roles { get; set; }

        public int TotalPages => (int)System.Math.Ceiling((double)TotalCount / PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}
