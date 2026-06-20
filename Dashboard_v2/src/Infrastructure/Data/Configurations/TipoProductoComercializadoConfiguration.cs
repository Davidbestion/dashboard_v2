using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard_v2.Infrastructure.Data.Configurations;

public class TipoProductoComercializadoConfiguration : IEntityTypeConfiguration<TipoProductoComercializado>
{
    public void Configure(EntityTypeBuilder<TipoProductoComercializado> builder)
    {
        builder.ToTable("TipoProductosComercializados");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasMaxLength(450);
        builder.Property(t => t.Nombre).IsRequired().HasMaxLength(500);
    }
}
