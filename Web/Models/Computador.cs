using System;
using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;

namespace Web.Models
{
    [FirestoreData]
    public class Computador
    {
        [FirestoreDocumentId]
        public string MAC { get; set; }

        [FirestoreProperty]
        public string? IP { get; set; }

        [FirestoreProperty]
        public string? ColaboradorCPF { get; set; }

        [FirestoreDocumentIgnore]
        public string? ColaboradorNome { get; set; }

        [FirestoreProperty]
        [Required(ErrorMessage = "O Hostname é obrigatório.")]
        public string Hostname { get; set; }

        [FirestoreProperty]
        public string? Fabricante { get; set; }

        [FirestoreProperty]
        public string? Processador { get; set; }

        [FirestoreProperty]
        public string? ProcessadorFabricante { get; set; }

        [FirestoreProperty]
        public string? ProcessadorCore { get; set; }

        [FirestoreProperty]
        public string? ProcessadorThread { get; set; }

        [FirestoreProperty]
        public string? ProcessadorClock { get; set; }

        [FirestoreProperty]
        public string? Ram { get; set; }

        [FirestoreProperty]
        public string? RamTipo { get; set; }

        [FirestoreProperty]
        public string? RamVelocidade { get; set; }

        [FirestoreProperty]
        public string? RamVoltagem { get; set; }

        [FirestoreProperty]
        public string? RamPorModule { get; set; }

        [FirestoreProperty]
        public string? ArmazenamentoC { get; set; }

        [FirestoreProperty]
        public string? ArmazenamentoCTotal { get; set; }

        [FirestoreProperty]
        public string? ArmazenamentoCLivre { get; set; }

        [FirestoreProperty]
        public string? ArmazenamentoD { get; set; }

        [FirestoreProperty]
        public string? ArmazenamentoDTotal { get; set; }

        [FirestoreProperty]
        public string? ArmazenamentoDLivre { get; set; }

        [FirestoreProperty]
        public string? ConsumoCPU { get; set; }

        [FirestoreProperty]
        public string? SO { get; set; }

        [FirestoreProperty]
        public DateTime? DataColeta { get; set; }

        [FirestoreProperty]
        public string? PartNumber { get; set; }
    }
}