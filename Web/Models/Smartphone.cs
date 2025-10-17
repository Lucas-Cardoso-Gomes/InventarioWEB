using System;
using System.ComponentModel.DataAnnotations;

namespace Web.Models
{
    public class Smartphone
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Modelo { get; set; }

        [Required]
        [StringLength(15)]
        public string IMEI1 { get; set; }

        [StringLength(15)]
        public string? IMEI2 { get; set; }

        [StringLength(100)]
        public string? Usuario { get; set; }

        [StringLength(100)]
        public string? Filial { get; set; }

        [DataType(DataType.Date)]
        public DateTime DataCriacao { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DataAlteracao { get; set; }

        [StringLength(100)]
        public string? ContaGoogle { get; set; }

        [StringLength(100)]
        public string? SenhaGoogle { get; set; }
    }
}