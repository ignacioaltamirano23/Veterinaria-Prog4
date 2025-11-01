using LogicaDeNegocio.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogicaDeNegocio.Context
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Veterinario> Veterinarios { get; set; }
        public DbSet<Mascota> Mascotas { get; set; }
        public DbSet<Turno> Turnos { get; set; }
        public DbSet<Rol> Roles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Roles iniciales
            modelBuilder.Entity<Rol>().HasData(
                new Rol { Id = 1, Nombre = "Administrador" },
                new Rol { Id = 2, Nombre = "Veterinario" },
                new Rol { Id = 3, Nombre = "Cliente" }
            );

            modelBuilder.Entity<Usuario>()
            .HasOne(u => u.Rol)
            .WithMany(r => r.Usuarios)
            .OnDelete(DeleteBehavior.Restrict); // Evita borrado en cascada de roles

            modelBuilder.Entity<Usuario>()
                .HasOne(u => u.Veterinario)
                .WithOne(v => v.Usuario)
                .HasForeignKey<Veterinario>(v => v.UsuarioId)
                .OnDelete(DeleteBehavior.NoAction); // Evita borrado en cascada de turnos

            // Turno: guardar enum como string
            modelBuilder.Entity<Turno>()
                .Property(t => t.EstadoTurno)
                .HasConversion<string>()
                .IsRequired();
        }
    }
}
