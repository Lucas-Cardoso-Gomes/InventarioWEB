using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Web.Models;

namespace Web.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Computador> Computadores { get; set; }
        public DbSet<Disco> Discos { get; set; }
        public DbSet<Gpu> GPUs { get; set; }
        public DbSet<AdaptadorRede> AdaptadoresRede { get; set; }
        public DbSet<Log> Logs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Define a chave primária para Computador
            modelBuilder.Entity<Computador>()
                .HasKey(c => c.MAC);

            // Configura as relações de um-para-muitos
            modelBuilder.Entity<Computador>()
                .HasMany(c => c.Discos)
                .WithOne(d => d.Computador)
                .HasForeignKey(d => d.ComputadorMAC)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Computador>()
                .HasMany(c => c.GPUs)
                .WithOne(g => g.Computador)
                .HasForeignKey(g => g.ComputadorMAC)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Computador>()
                .HasMany(c => c.AdaptadoresRede)
                .WithOne(a => a.Computador)
                .HasForeignKey(a => a.ComputadorMAC)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
