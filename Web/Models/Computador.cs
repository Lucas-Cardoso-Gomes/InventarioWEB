using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Web.Models
{
    public class Computador
    {
        [Key]
        [Required]
        public string MAC { get; set; }

        public string IP { get; set; }
        public string Usuario { get; set; }
        public string Hostname { get; set; }
        public string Fabricante { get; set; }
        public string SO { get; set; }
        public DateTime? DataColeta { get; set; }

        // Processador
        public string ProcessadorNome { get; set; }
        public string ProcessadorFabricante { get; set; }
        public int ProcessadorCores { get; set; }
        public int ProcessadorThreads { get; set; }
        public string ProcessadorClock { get; set; }

        // RAM
        public string RamTotal { get; set; }
        public string RamTipo { get; set; }
        public string RamVelocidade { get; set; }
        public string RamVoltagem { get; set; }
        public string RamPorModulo { get; set; }

        // Consumo
        public string ConsumoCPU { get; set; }

        // Navigation Properties
        public virtual ICollection<Disco> Discos { get; set; } = new List<Disco>();
        public virtual ICollection<Gpu> GPUs { get; set; } = new List<Gpu>();
        public virtual ICollection<AdaptadorRede> AdaptadoresRede { get; set; } = new List<AdaptadorRede>();
    }
}