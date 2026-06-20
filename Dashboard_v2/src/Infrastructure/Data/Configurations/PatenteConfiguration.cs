using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard_v2.Infrastructure.Data.Configurations;

public class PatenteConfiguration : IEntityTypeConfiguration<Patente>
{
    public void Configure(EntityTypeBuilder<Patente> builder)
    {
        builder.ToTable("Patentes");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasMaxLength(450);
        builder.Property(p => p.Titulo).IsRequired().HasMaxLength(1000);
        builder.Property(p => p.NumeroSolicitudConcesion).HasMaxLength(500);
        // Patente no está relacionada con Institution según el MER
    }
}
