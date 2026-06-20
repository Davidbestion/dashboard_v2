using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard_v2.Infrastructure.Data.Configurations;

public class TipoNormaConfiguration : IEntityTypeConfiguration<TipoNorma>
{
    public void Configure(EntityTypeBuilder<TipoNorma> builder)
    {
        builder.ToTable("TiposNorma");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Nombre).IsRequired().HasMaxLength(500);
    }
}
