using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard_v2.Infrastructure.Data.Configurations;

public class ClasificacionConfiguration : IEntityTypeConfiguration<Clasificacion>
{
    public void Configure(EntityTypeBuilder<Clasificacion> builder)
    {
        builder.ToTable("Clasificaciones");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasMaxLength(450);
        builder.Property(c => c.Nombre).IsRequired().HasMaxLength(200);
    }
}
