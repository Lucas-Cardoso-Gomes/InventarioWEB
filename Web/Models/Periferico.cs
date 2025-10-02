using System;
using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;

namespace Web.Models
{
    [FirestoreData]
    public class Periferico
    {
        [FirestoreDocumentId]
        [Required(ErrorMessage = "O Part Number é obrigatório.")]
        public string PartNumber { get; set; }

        [FirestoreProperty]
        [Display(Name = "Colaborador")]
        public string? ColaboradorCPF { get; set; }

        [FirestoreDocumentIgnore]
        public virtual Colaborador? Colaborador { get; set; }

        [FirestoreDocumentIgnore]
        public string? ColaboradorNome { get; set; }

        [FirestoreProperty]
        [Required(ErrorMessage = "O Tipo é obrigatório.")]
        public string Tipo { get; set; }

        [FirestoreProperty]
        [Display(Name = "Data de Entrega")]
        [DataType(DataType.Date)]
        public DateTime? DataEntrega { get; set; }
    }
}