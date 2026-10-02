using System.Collections.Generic;

namespace Web.Models
{
    public class ProgramasIndexViewModel
    {
        public List<ProgramaAgrupado> ProgramasAgrupados { get; set; } = new List<ProgramaAgrupado>();
        
        // Listas de opções disponíveis
        public List<string> NomesDisponiveis { get; set; } = new List<string>();
        public List<string> QuantidadesDisponiveis { get; set; } = new List<string>();

        // Filtros selecionados
        public List<string> SelectedNomes { get; set; } = new List<string>();
        public List<string> SelectedQuantidades { get; set; } = new List<string>();
    }

    public class ProgramaAgrupado
    {
        public string Nome { get; set; }
        public string Desenvolvedor { get; set; }
        public int Quantidade { get; set; }
        public List<ProgramaInstalacao> Instalacoes { get; set; } = new List<ProgramaInstalacao>();
    }

    public class ProgramaInstalacao
    {
        public string Hostname { get; set; }
        public string ComputadorMAC { get; set; }
        public string Versao { get; set; }
        public string DataColeta { get; set; }
    }
}
