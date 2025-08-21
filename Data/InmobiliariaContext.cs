using Inmobiliaria_troncoso_leandro.Models;
using Microsoft.EntityFrameworkCore;

namespace Inmobiliaria_troncoso_leandro.Data
{
    public class InmobiliariaContext : DbContext
    {
        public InmobiliariaContext(DbContextOptions<InmobiliariaContext> options) : base(options) { }

        // Tablas de la base de datos
        public DbSet<Propietario> Propietarios { get; set; }
        public DbSet<Inquilino> Inquilinos { get; set; }
        //public DbSet<Inmueble> Inmuebles { get; set; }
        //public DbSet<Contrato> Contratos { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configuraciones adicionales de modelos
            modelBuilder.Entity<Propietario>().HasIndex(p => p.Dni).IsUnique();
            modelBuilder.Entity<Inquilino>().HasIndex(i => i.Dni).IsUnique();


        }
    }
}