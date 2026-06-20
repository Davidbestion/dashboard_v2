using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard_v2.Infrastructure.Data.Configurations;

public class ProductoComercializadoConfiguration : IEntityTypeConfiguration<ProductoComercializado>
{
    public void Configure(EntityTypeBuilder<ProductoComercializado> builder)
    {
        builder.ToTable("ProductosComercializados");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasMaxLength(450);
        builder.Property(p => p.Titulo).IsRequired().HasMaxLength(1000);

        builder.HasOne(p => p.TipoProductoComercializado)
            .WithMany(t => t.Productos)
            .HasForeignKey(p => p.TipoProductoComercializadoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Institution)
            .WithMany(i => i.ProductosComercializados)
            .HasForeignKey(p => p.InstitutionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
