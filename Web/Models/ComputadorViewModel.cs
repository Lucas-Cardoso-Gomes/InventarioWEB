using System;
using System.ComponentModel.DataAnnotations;

namespace web.Models
{
    public class ComputadorViewModel
    {
        [Required(ErrorMessage = "O endereço MAC é obrigatório.")]
        [Display(Name = "MAC Address")]
        [RegularExpression(@"^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$", ErrorMessage = "Formato de MAC Address inválido.")]
        public string MAC { get; set; }

        [Display(Name = "Endereço IP")]
        public string IP { get; set; }

        [Display(Name = "Usuário")]
        public string Usuario { get; set; }

        [Display(Name = "Hostname")]
        public string Hostname { get; set; }

        [Display(Name = "Fabricante")]
        public string Fabricante { get; set; }

        [Display(Name = "Processador")]
        public string Processador { get; set; }

        [Display(Name = "Fabricante do Processador")]
        public string ProcessadorFabricante { get; set; }

        [Display(Name = "Cores do Processador")]
        public string ProcessadorCore { get; set; }

        [Display(Name = "Threads do Processador")]
        public string ProcessadorThread { get; set; }

        [Display(Name = "Clock do Processador")]
        public string ProcessadorClock { get; set; }

        [Display(Name = "RAM Total")]
        public string Ram { get; set; }

        [Display(Name = "Tipo de RAM")]
        public string RamTipo { get; set; }

        [Display(Name = "Velocidade da RAM")]
        public string RamVelocidade { get; set; }

        [Display(Name = "Voltagem da RAM")]
        public string RamVoltagem { get; set; }

        [Display(Name = "RAM por Módulo")]
        public string RamPorModule { get; set; }

        [Display(Name = "Drive C:")]
        public string ArmazenamentoC { get; set; }

        [Display(Name = "Total C:")]
        public string ArmazenamentoCTotal { get; set; }

        [Display(Name = "Livre C:")]
        public string ArmazenamentoCLivre { get; set; }

        [Display(Name = "Drive D:")]
        public string ArmazenamentoD { get; set; }

        [Display(Name = "Total D:")]
        public string ArmazenamentoDTotal { get; set; }

        [Display(Name = "Livre D:")]
        public string ArmazenamentoDLivre { get; set; }

        [Display(Name = "Consumo de CPU (%)")]
        public string ConsumoCPU { get; set; }

        [Display(Name = "Sistema Operacional")]
        public string SO { get; set; }
    }
}
