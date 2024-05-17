using Microsoft.EntityFrameworkCore;
using Banco.Models;

namespace Banco.Data
{
    public class DBContext : DbContext
    {
        public DBContext(DbContextOptions<DBContext> options) : base(options)
        {

        }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Movimientos> Movimientos { get; set; }
        public DbSet<TipoMovimiento> TiposMovimiento { get; set; }
        public DbSet<Cuenta> Cuentas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Usuario>(tb =>
            {
                tb.HasKey(col => col.IdUsuario);
                tb.Property(col => col.IdUsuario).UseIdentityColumn()
                .ValueGeneratedOnAdd();

                tb.Property(col => col.Nombre).HasMaxLength(50);
                tb.Property(col => col.Correo).HasMaxLength(50);
                tb.Property(col => col.Clave).HasMaxLength(50);
                tb.Property(col => col.Identificacion).HasMaxLength(50);
            });

            modelBuilder.Entity<Movimientos>(tb =>
            {
                tb.HasKey(col => col.IdMovimiento);

                tb.Property(col => col.IdMovimiento).UseIdentityColumn().ValueGeneratedOnAdd();
                tb.HasOne(m => m.Usuario)
                  .WithMany(u => u.Movimientos)
                  .HasForeignKey(m => m.IdUsuario);

                tb.HasOne(m => m.TipoMovimiento)
                  .WithMany()
                  .HasForeignKey(m => m.IdTipoMovimiento);

                tb.Property(m => m.Cantidad)
               .IsRequired()
                .HasColumnType("decimal(18,2)");
                tb.Property(m => m.Fecha).IsRequired();
            });

            modelBuilder.Entity<TipoMovimiento>(tb =>
            {
                tb.HasKey(col => col.IdTipoMovimiento);
                tb.Property(col => col.IdTipoMovimiento).UseIdentityColumn().ValueGeneratedOnAdd();
                tb.Property(col => col.Nombre).IsRequired();

                modelBuilder.Entity<TipoMovimiento>().HasData(
                new TipoMovimiento { IdTipoMovimiento = 1, Nombre = "Retirar" },
                new TipoMovimiento { IdTipoMovimiento = 2, Nombre = "Consultar Saldo" },
                new TipoMovimiento { IdTipoMovimiento = 3, Nombre = "Consignar" },
                new TipoMovimiento { IdTipoMovimiento = 4, Nombre = "Consultar Movimientos" },
                new TipoMovimiento { IdTipoMovimiento = 5, Nombre = "Depositar" }
                ) ;

            });

            modelBuilder.Entity<Cuenta>(tb =>
            {
                tb.HasKey(col => col.IdCuenta);
                tb.Property(col => col.IdCuenta).UseIdentityColumn().ValueGeneratedOnAdd();
                tb.HasOne(c => c.Usuario)
                  .WithOne(u => u.Cuenta)
                  .HasForeignKey<Cuenta>(c => c.IdUsuario);
                tb.Property(c => c.Saldo).HasColumnType("decimal(18,2)").IsRequired();
            });

            modelBuilder.Entity<Usuario>().ToTable("Usuario");
            modelBuilder.Entity<Movimientos>().ToTable("Movimientos");
            modelBuilder.Entity<TipoMovimiento>().ToTable("TipoMovimiento");
            modelBuilder.Entity<Cuenta>().ToTable("Cuenta");

        }
    }
}