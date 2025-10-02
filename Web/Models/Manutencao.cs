using System;
using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;

namespace Web.Models
{
    [FirestoreData]
    public class Manutencao
    {
        [FirestoreDocumentId]
        public string Id { get; set; }

        [FirestoreProperty]
        [Display(Name = "MAC do Computador")]
        public string? ComputadorMAC { get; set; }

        [FirestoreDocumentIgnore]
        public Computador? Computador { get; set; }

        [FirestoreProperty]
        [Display(Name = "PartNumber do Monitor")]
        public string? MonitorPartNumber { get; set; }

        [FirestoreDocumentIgnore]
        public Monitor? Monitor { get; set; }

        [FirestoreProperty]
        [Display(Name = "PartNumber do Periférico")]
        public string? PerifericoPartNumber { get; set; }

        [FirestoreDocumentIgnore]
        public Periferico? Periferico { get; set; }

        [FirestoreProperty]
        [Display(Name = "Data de Manutenção de Hardware")]
        [DataType(DataType.Date)]
        public DateTime? DataManutencaoHardware { get; set; }

        [FirestoreProperty]
        [Display(Name = "Data de Manutenção de Software")]
        [DataType(DataType.Date)]
        public DateTime? DataManutencaoSoftware { get; set; }

        [FirestoreProperty]
        [Display(Name = "Manutenção Externa")]
        public string? ManutencaoExterna { get; set; }

        [FirestoreProperty]
        [DataType(DataType.Date)]
        public DateTime? Data { get; set; }

        [FirestoreProperty]
        public DateTime? DataAlteracao { get; set; }

        [FirestoreProperty]
        [Display(Name = "Histórico")]
        public string? Historico { get; set; }
    }
}