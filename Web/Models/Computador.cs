using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Web.Models;

namespace web.Models
{
    public class Computador
    {
        [Key]
        [Required(ErrorMessage = "O endereço MAC é obrigatório.")]
        public string MAC { get; set; }
        public string? IP { get; set; }
        public int? UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
        public string? ColaboradorNome { get; set; }
        [Required(ErrorMessage = "O Hostname é obrigatório.")]
        public string Hostname { get; set; }
        public string? Fabricante { get; set; }
        public string? Processador { get; set; }
        public string? ProcessadorFabricante { get; set; }
        public string? ProcessadorCore { get; set; }
        public string? ProcessadorThread { get; set; }
        public string? ProcessadorClock { get; set; }
        public string? Ram { get; set; }
        public string? RamTipo { get; set; }
        public string? RamVelocidade { get; set; }
        public string? RamVoltagem { get; set; }
        public string? RamPorModule { get; set; }
        public string? ArmazenamentoC { get; set; }
        public string? ArmazenamentoCTotal { get; set; }
        public string? ArmazenamentoCLivre { get; set; }
        public string? ArmazenamentoD { get; set; }
        public string? ArmazenamentoDTotal { get; set; }
        public string? ArmazenamentoDLivre { get; set; }
        public string? ConsumoCPU { get; set; }
        public string? SO { get; set; }
        public DateTime? DataColeta { get; set; }
        public List<Manutencao>? Manutencoes { get; set; }
    }
}