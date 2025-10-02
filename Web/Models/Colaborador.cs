using System;
using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;

namespace Web.Models
{
    [FirestoreData]
    public class Colaborador
    {
        [FirestoreDocumentId]
        public string CPF { get; set; }

        [FirestoreProperty]
        [Required(ErrorMessage = "O Nome é obrigatório.")]
        public string Nome { get; set; }

        [FirestoreProperty]
        [EmailAddress(ErrorMessage = "E-mail inválido.")]
        public string? Email { get; set; }
        
        [FirestoreProperty]
        public string? SenhaEmail { get; set; }

        [FirestoreProperty]
        public string? Teams { get; set; }

        [FirestoreProperty]
        public string? SenhaTeams { get; set; }

        [FirestoreProperty]
        public string? EDespacho { get; set; }

        [FirestoreProperty]
        public string? SenhaEDespacho { get; set; }

        [FirestoreProperty]
        public string? Genius { get; set; }

        [FirestoreProperty]
        public string? SenhaGenius { get; set; }

        [FirestoreProperty]
        public string? Ibrooker { get; set; }

        [FirestoreProperty]
        public string? SenhaIbrooker { get; set; }

        [FirestoreProperty]
        public string? Adicional { get; set; }

        [FirestoreProperty]
        public string? SenhaAdicional { get; set; }

        [FirestoreProperty]
        public string? Filial { get; set; }

        [FirestoreProperty]
        public string? Setor { get; set; }

        [FirestoreProperty]
        public string? Smartphone { get; set; }

        [FirestoreProperty]
        public string? TelefoneFixo { get; set; }

        [FirestoreProperty]
        public string? Ramal { get; set; }

        [FirestoreProperty]
        public string? Alarme { get; set; }

        [FirestoreProperty]
        public string? Videoporteiro { get; set; }
        
        [FirestoreProperty]
        public string? Obs { get; set; }

        [FirestoreProperty]
        public DateTime? DataInclusao { get; set; }

        [FirestoreProperty]
        public DateTime? DataAlteracao { get; set; }

        [FirestoreProperty]
        [Display(Name = "Coordenador")]
        public string? CoordenadorCPF { get; set; }
        
        [FirestoreDocumentIgnore]
        public string? CoordenadorNome { get; set; }
    }
}