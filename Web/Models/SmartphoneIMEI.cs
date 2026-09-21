using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web.Models
{
    public class SmartphoneIMEI
    {
        [Key]
        public int Id { get; set; }

        public int SmartphoneId { get; set; }

        [Required]
        [StringLength(50)]
        public string IMEI { get; set; }

        public int Ordem { get; set; } = 1;

        [ForeignKey("SmartphoneId")]
        public virtual Smartphone? Smartphone { get; set; }
    }
}
