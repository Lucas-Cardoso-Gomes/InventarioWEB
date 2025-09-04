using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace Web.Models
{
    public class ManutencaoIndexViewModel
    {
        public List<Manutencao> Manutencoes { get; set; }
        public string Cpf { get; set; }
        public string ComputadorMAC { get; set; }
        public SelectList Computadores { get; set; }
    }
}
