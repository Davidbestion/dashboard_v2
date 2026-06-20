using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard_v2.Infrastructure.Data.Configurations;

public class AuthorProductoComercializadoConfiguration : IEntityTypeConfiguration<AuthorProductoComercializado>
{
    public void Configure(EntityTypeBuilder<AuthorProductoComercializado> builder)
    {
        builder.ToTable("AuthorProductosComercializados");

        builder.HasKey(ap => new { ap.AuthorId, ap.ProductoComercializadoId });

        builder.Property(ap => ap.AuthorId).HasMaxLength(450);
        builder.Property(ap => ap.ProductoComercializadoId).HasMaxLength(450);

        builder.HasOne(ap => ap.Author)
            .WithMany(a => a.AuthorProductosComercializados)
            .HasForeignKey(ap => ap.AuthorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ap => ap.ProductoComercializado)
            .WithMany(p => p.Creadores)
            .HasForeignKey(ap => ap.ProductoComercializadoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
