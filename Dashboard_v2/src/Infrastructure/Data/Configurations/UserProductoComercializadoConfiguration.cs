using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard_v2.Infrastructure.Data.Configurations;

public class UserProductoComercializadoConfiguration : IEntityTypeConfiguration<UserProductoComercializado>
{
    public void Configure(EntityTypeBuilder<UserProductoComercializado> builder)
    {
        builder.ToTable("UserProductosComercializados");

        builder.HasKey(up => new { up.UserId, up.ProductoComercializadoId });

        builder.Property(up => up.UserId).HasMaxLength(450);
        builder.Property(up => up.ProductoComercializadoId).HasMaxLength(450);

        builder.HasOne(up => up.User)
            .WithMany(u => u.ProductosComercializadosCreados)
            .HasForeignKey(up => up.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(up => up.ProductoComercializado)
            .WithMany()
            .HasForeignKey(up => up.ProductoComercializadoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
