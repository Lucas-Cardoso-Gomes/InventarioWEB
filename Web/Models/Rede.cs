using System;
using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;

namespace Web.Models
{
    [FirestoreData]
    public class Rede
    {
        [FirestoreDocumentId]
        public string Id { get; set; }

        [FirestoreProperty]
        [Required(ErrorMessage = "O campo Tipo é obrigatório.")]
        public string Tipo { get; set; } // Roteador, Switch, AP

        [FirestoreProperty]
        [Required(ErrorMessage = "O campo IP é obrigatório.")]
        [Display(Name = "Endereço IP")]
        public string IP { get; set; }

        [FirestoreProperty]
        [Display(Name = "Endereço MAC")]
        public string? MAC { get; set; }

        [FirestoreProperty]
        [Required(ErrorMessage = "O campo Nome é obrigatório.")]
        public string Nome { get; set; }

        [FirestoreProperty]
        [Display(Name = "Data de Inclusão")]
        public DateTime DataInclusao { get; set; }

        [FirestoreProperty]
        [Display(Name = "Data de Alteração")]
        public DateTime? DataAlteracao { get; set; }

        [FirestoreProperty]
        [Display(Name = "Observação")]
        public string? Observacao { get; set; }

        // Monitoring properties
        [FirestoreDocumentIgnore]
        public string? Status { get; set; }
        [FirestoreDocumentIgnore]
        public double LossPercentage { get; set; }
        [FirestoreDocumentIgnore]
        public int PingCount { get; set; }
        [FirestoreDocumentIgnore]
        public double AverageLatency { get; set; }
    }
}