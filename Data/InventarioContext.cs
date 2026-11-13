using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using proyecto.Models;

namespace proyecto.Data;

public partial class InventarioContext : DbContext
{
    public InventarioContext(DbContextOptions<InventarioContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Consola> Consolas { get; set; }

    public virtual DbSet<Stock> Stocks { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    public virtual DbSet<VentaConsola> VentaConsolas { get; set; }

    public virtual DbSet<Ventum> Venta { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Consola>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__consolas__3213E83FED49D065");

            entity.ToTable("consolas");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Anio).HasColumnName("anio");
            entity.Property(e => e.Generacion)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("generacion");
            entity.Property(e => e.Imagen)
                .HasMaxLength(1000)
                .IsUnicode(false)
                .HasColumnName("imagen");
            entity.Property(e => e.Modelo)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("modelo");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("nombre");
            entity.Property(e => e.Region)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("region");
        });

        modelBuilder.Entity<Stock>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__stock__3213E83FB05BA3AF");

            entity.ToTable("stock");

            entity.HasIndex(e => e.IdConsola, "UQ_stock_consola").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Existencias).HasColumnName("existencias");
            entity.Property(e => e.IdConsola).HasColumnName("id_consola");
            entity.Property(e => e.PrecioUnitario)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("precio_unitario");

            entity.HasOne(d => d.IdConsolaNavigation).WithOne(p => p.Stock)
                .HasForeignKey<Stock>(d => d.IdConsola)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_stock_consola");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__usuario__3213E83F6A894FC6");

            entity.ToTable("usuario");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Activo)
                .HasDefaultValue(true, "DF_usuario_activo")
                .HasColumnName("activo");
            entity.Property(e => e.Contrasenia)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("contrasenia");
            entity.Property(e => e.Correo)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("correo");
            entity.Property(e => e.Esadmin)
                .HasDefaultValue(false, "DF_usuario_esadmin")
                .HasColumnName("esadmin");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("nombre");
            entity.Property(e => e.Telefono)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("telefono");
        });

        modelBuilder.Entity<VentaConsola>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__venta_co__3213E83F0489AA14");

            entity.ToTable("venta_consola");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Cantidad).HasColumnName("cantidad");
            entity.Property(e => e.Folio).HasColumnName("folio");
            entity.Property(e => e.IdConsola).HasColumnName("id_consola");
            entity.Property(e => e.Subtotal)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("subtotal");

            entity.HasOne(d => d.FolioNavigation).WithMany(p => p.VentaConsolas)
                .HasForeignKey(d => d.Folio)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_vc_venta");

            entity.HasOne(d => d.IdConsolaNavigation).WithMany(p => p.VentaConsolas)
                .HasForeignKey(d => d.IdConsola)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_vc_consola");
        });

        modelBuilder.Entity<Ventum>(entity =>
        {
            entity.HasKey(e => e.Folio).HasName("PK__venta__E8F12C9E0DB9B209");

            entity.ToTable("venta");

            entity.Property(e => e.Folio).HasColumnName("folio");
            entity.Property(e => e.IdUsuario)
                .HasDefaultValue(1)
                .HasColumnName("id_usuario");
            entity.Property(e => e.Subtotal)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("subtotal");
            entity.Property(e => e.Total)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("total");

            entity.HasOne(d => d.IdUsuarioNavigation).WithMany(p => p.Venta)
                .HasForeignKey(d => d.IdUsuario)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_venta_usuario");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
