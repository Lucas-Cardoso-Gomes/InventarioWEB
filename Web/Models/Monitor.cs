using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;

namespace Web.Models
{
    [FirestoreData]
    public class Monitor
    {
        [FirestoreDocumentId]
        [Required(ErrorMessage = "O Part Number é obrigatório.")]
        public string PartNumber { get; set; }

        [FirestoreProperty]
        [Display(Name = "Colaborador")]
        public string? ColaboradorCPF { get; set; }

        [FirestoreDocumentIgnore]
        public string? ColaboradorNome { get; set; }

        [FirestoreDocumentIgnore]
        public virtual Colaborador? Colaborador { get; set; }

        [FirestoreProperty]
        public string? Marca { get; set; }

        [FirestoreProperty]
        [Required(ErrorMessage = "O Modelo é obrigatório.")]
        public string Modelo { get; set; }

        [FirestoreProperty]
        public string Tamanho { get; set; }
    }
}