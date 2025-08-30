using System;
using System.ComponentModel.DataAnnotations;

namespace Web.Models
{
    public class Log
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }

        [Required]
        [MaxLength(10)]
        public string Level { get; set; } // e.g., "Info", "Warning", "Error"

        [Required]
        public string Message { get; set; }

        [MaxLength(50)]
        public string Source { get; set; } // e.g., "Coleta", "Comandos"
    }
}
