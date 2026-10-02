using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace Web.Models
{
    public class ManutencaoIndexViewModel
    {
        public List<Manutencao> Manutencoes { get; set; }
        public string PartNumber { get; set; }
        public string Colaborador { get; set; }
        public string Hostname { get; set; }
    }
}
