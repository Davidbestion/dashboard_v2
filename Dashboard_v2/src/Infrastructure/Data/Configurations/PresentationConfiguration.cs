using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard_v2.Infrastructure.Data.Configurations;

public class PresentationConfiguration : IEntityTypeConfiguration<Presentation>
{
    public void Configure(EntityTypeBuilder<Presentation> builder)
    {
        // TPT: tabla propia solo con las columnas específicas de Presentation.
        // La PK (Id) y las columnas comunes (UserId, EventId, Fecha) viven en ParticipacionesEnEvento.
        builder.ToTable("Presentations");
        builder.Property(p => p.Name).IsRequired().HasMaxLength(500);
    }
}
