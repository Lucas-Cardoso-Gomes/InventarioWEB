using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Web.Models
{
    public enum Role
    {
        Normal,
        Coordenador,
        Diretoria,
        Admin
    }

    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "O Nome é obrigatório.")]
        [StringLength(100)]
        public string Nome { get; set; }

        [Required(ErrorMessage = "O Login é obrigatório.")]
        [StringLength(50)]
        public string Login { get; set; }

        [Required(ErrorMessage = "A Senha é obrigatória.")]
        public string PasswordHash { get; set; }

        [Required]
        public Role Role { get; set; }

        [Required(ErrorMessage = "O CPF é obrigatório.")]
        [StringLength(14)]
        public string CPF { get; set; }

        [EmailAddress(ErrorMessage = "E-mail inválido.")]
        [StringLength(100)]
        [ValidateNever]
        public string? Email { get; set; }

        [StringLength(100)]
        [ValidateNever]
        public string? SenhaEmail { get; set; }

        [StringLength(100)]
        [ValidateNever]
        public string? Teams { get; set; }

        [StringLength(100)]
        [ValidateNever]
        public string? SenhaTeams { get; set; }

        [StringLength(100)]
        [ValidateNever]
        public string? EDespacho { get; set; }

        [StringLength(100)]
        [ValidateNever]
        public string? SenhaEDespacho { get; set; }

        [StringLength(100)]
        [ValidateNever]
        public string? Genius { get; set; }

        [StringLength(100)]
        [ValidateNever]
        public string? SenhaGenius { get; set; }

        [StringLength(100)]
        [ValidateNever]
        public string? Ibrooker { get; set; }

        [StringLength(100)]
        [ValidateNever]
        public string? SenhaIbrooker { get; set; }

        [StringLength(100)]
        [ValidateNever]
        public string? Adicional { get; set; }

        [StringLength(100)]
        [ValidateNever]
        public string? SenhaAdicional { get; set; }

        [StringLength(100)]
        [ValidateNever]
        public string? Setor { get; set; }

        [StringLength(100)]
        [ValidateNever]
        public string? Smartphone { get; set; }

        [StringLength(100)]
        [ValidateNever]
        public string? TelefoneFixo { get; set; }

        [StringLength(100)]
        [ValidateNever]
        public string? Ramal { get; set; }

        [StringLength(100)]
        [ValidateNever]
        public string? Alarme { get; set; }

        [StringLength(100)]
        [ValidateNever]
        public string? Videoporteiro { get; set; }

        [ValidateNever]
        public string? Obs { get; set; }

        [ValidateNever]
        public DateTime? DataInclusao { get; set; }

        [ValidateNever]
        public DateTime? DataAlteracao { get; set; }

        // Self-referencing relationship for Coordinator
        public int? CoordenadorId { get; set; }
        [ForeignKey("CoordenadorId")]
        [ValidateNever]
        public User? Coordenador { get; set; }

        [ValidateNever]
        public ICollection<User> Subordinados { get; set; } = new List<User>();

        // Navigation properties for devices
        [ValidateNever]
        public ICollection<Computador> Computadores { get; set; } = new List<Computador>();
        [ValidateNever]
        public ICollection<Monitor> Monitores { get; set; } = new List<Monitor>();
        [ValidateNever]
        public ICollection<Periferico> Perifericos { get; set; } = new List<Periferico>();
    }
}
