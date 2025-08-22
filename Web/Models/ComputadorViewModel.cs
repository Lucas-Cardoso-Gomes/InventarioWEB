using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace web.Models
{
    public class ComputadorViewModel
    {
        [Required(ErrorMessage = "O endereço MAC é obrigatório.")]
        [Display(Name = "MAC Address")]
        [RegularExpression(@"^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$", ErrorMessage = "Formato de MAC Address inválido.")]
        public string MAC { get; set; }

        [Display(Name = "Endereço IP")]
        [ValidateNever]
        public string IP { get; set; }

        [Display(Name = "Colaborador")]
        [ValidateNever]
        public string ColaboradorNome { get; set; }

        [Display(Name = "Hostname")]
        [Required(ErrorMessage = "O Hostname é obrigatório.")]
        public string Hostname { get; set; }

        [Display(Name = "Fabricante")]
        [ValidateNever]
        public string Fabricante { get; set; }

        [Display(Name = "Processador")]
        [ValidateNever]
        public string Processador { get; set; }

        [Display(Name = "Fabricante do Processador")]
        [ValidateNever]
        public string ProcessadorFabricante { get; set; }

        [Display(Name = "Cores do Processador")]
        [ValidateNever]
        public string ProcessadorCore { get; set; }

        [Display(Name = "Threads do Processador")]
        [ValidateNever]
        public string ProcessadorThread { get; set; }

        [Display(Name = "Clock do Processador")]
        [ValidateNever]
        public string ProcessadorClock { get; set; }

        [Display(Name = "RAM Total")]
        [ValidateNever]
        public string Ram { get; set; }

        [Display(Name = "Tipo de RAM")]
        [ValidateNever]
        public string RamTipo { get; set; }

        [Display(Name = "Velocidade da RAM")]
        [ValidateNever]
        public string RamVelocidade { get; set; }

        [Display(Name = "Voltagem da RAM")]
        [ValidateNever]
        public string RamVoltagem { get; set; }

        [Display(Name = "RAM por Módulo")]
        [ValidateNever]
        public string RamPorModule { get; set; }

        [Display(Name = "Drive C:")]
        [ValidateNever]
        public string ArmazenamentoC { get; set; }

        [Display(Name = "Total C:")]
        [ValidateNever]
        public string ArmazenamentoCTotal { get; set; }

        [Display(Name = "Livre C:")]
        [ValidateNever]
        public string ArmazenamentoCLivre { get; set; }

        [Display(Name = "Drive D:")]
        [ValidateNever]
        public string ArmazenamentoD { get; set; }

        [Display(Name = "Total D:")]
        [ValidateNever]
        public string ArmazenamentoDTotal { get; set; }

        [Display(Name = "Livre D:")]
        [ValidateNever]
        public string ArmazenamentoDLivre { get; set; }

        [Display(Name = "Consumo de CPU (%)")]
        [ValidateNever]
        public string ConsumoCPU { get; set; }

        [Display(Name = "Sistema Operacional")]
        [ValidateNever]
        public string SO { get; set; }
    }
}
