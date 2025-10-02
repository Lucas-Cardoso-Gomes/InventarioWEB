using System;
using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;

namespace Web.Models
{
    [FirestoreData]
    public class Chamado
    {
        [FirestoreDocumentId]
        public string ID { get; set; }

        [FirestoreProperty]
        [Display(Name = "Admin")]
        public string? AdminCPF { get; set; }

        [FirestoreProperty]
        [Required(ErrorMessage = "O CPF do colaborador é obrigatório.")]
        [Display(Name = "Colaborador")]
        public string ColaboradorCPF { get; set; }

        [FirestoreProperty]
        [Required(ErrorMessage = "O serviço é obrigatório.")]
        [StringLength(100, ErrorMessage = "O serviço não pode ter mais de 100 caracteres.")]
        public string Servico { get; set; }

        [FirestoreProperty]
        [Required(ErrorMessage = "A descrição é obrigatória.")]
        [StringLength(1000, ErrorMessage = "A descrição não pode ter mais de 1000 caracteres.")]
        public string Descricao { get; set; }

        [FirestoreProperty]
        [Display(Name = "Data de Alteração")]
        public DateTime? DataAlteracao { get; set; }

        [FirestoreProperty]
        [Display(Name = "Data de Criação")]
        public DateTime DataCriacao { get; set; }

        [FirestoreProperty]
        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Aberto";

        [FirestoreDocumentIgnore]
        public string? AdminNome { get; set; }

        [FirestoreDocumentIgnore]
        public string? ColaboradorNome { get; set; }
    }
}