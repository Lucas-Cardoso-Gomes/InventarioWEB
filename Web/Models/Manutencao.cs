using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace web.Models
{
    public class Manutencao
    {
        [Key]
        public int Id { get; set; }

        [Display(Name = "MAC do Computador")]
        public string? ComputadorMAC { get; set; }

        [ForeignKey("ComputadorMAC")]
        public Computador? Computador { get; set; }

        [Display(Name = "PartNumber do Monitor")]
        public string? MonitorPartNumber { get; set; }

        [ForeignKey("MonitorPartNumber")]
        public Monitor? Monitor { get; set; }

        [Display(Name = "PartNumber do Periférico")]
        public string? PerifericoPartNumber { get; set; }

        [ForeignKey("PerifericoPartNumber")]
        public Periferico? Periferico { get; set; }

        [Display(Name = "Data de Manutenção de Hardware")]
        [DataType(DataType.Date)]
        public DateTime? DataManutencaoHardware { get; set; }

        [Display(Name = "Data de Manutenção de Software")]
        [DataType(DataType.Date)]
        public DateTime? DataManutencaoSoftware { get; set; }

        [Display(Name = "Manutenção Externa")]
        public string? ManutencaoExterna { get; set; }

        [DataType(DataType.Date)]
        public DateTime? Data { get; set; }

        [Display(Name = "Histórico")]
        public string? Historico { get; set; }
    }
}
