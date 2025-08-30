using System;
using System.ComponentModel.DataAnnotations;

namespace Web.Models
{
    public class PersistentLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }

        [Required]
        [MaxLength(50)]
        public string EntityType { get; set; } // "User" or "Computer"

        [Required]
        [MaxLength(50)]
        public string ActionType { get; set; } // "Create", "Update", "Delete"

        [Required]
        [MaxLength(255)]
        public string PerformedBy { get; set; }

        public string Details { get; set; }
    }
}
