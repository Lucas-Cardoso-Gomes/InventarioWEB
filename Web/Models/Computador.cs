using System;
using System.ComponentModel.DataAnnotations;

namespace web.Models
{
    public class Computador
    {
        [Required]
        public string MAC { get; set; }
        public string IP { get; set; }
        public string ColaboradorNome { get; set; }
        public string Hostname { get; set; }
        public string Fabricante { get; set; }
        public string Processador { get; set; }
        public string ProcessadorFabricante { get; set; }
        public string ProcessadorCore { get; set; }
        public string ProcessadorThread { get; set; }
        public string ProcessadorClock { get; set; }
        public string Ram { get; set; }
        public string RamTipo { get; set; }
        public string RamVelocidade { get; set; }
        public string RamVoltagem { get; set; }
        public string RamPorModule { get; set; }
        public string ArmazenamentoC { get; set; }
        public string ArmazenamentoCTotal { get; set; }
        public string ArmazenamentoCLivre { get; set; }
        public string ArmazenamentoD { get; set; }
        public string ArmazenamentoDTotal { get; set; }
        public string ArmazenamentoDLivre { get; set; }
        public string ConsumoCPU { get; set; }
        public string SO { get; set; }
        public DateTime? DataColeta { get; set; }
    }
}