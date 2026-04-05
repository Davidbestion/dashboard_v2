using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard_v2.Infrastructure.Data.Configurations;

public class AreaConfiguration : IEntityTypeConfiguration<Area>
{
    public void Configure(EntityTypeBuilder<Area> builder)
    {
        builder.ToTable("Areas");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasMaxLength(450);
        builder.Property(a => a.Nombre).IsRequired().HasMaxLength(500);
        builder.Property(a => a.Descripcion);
        builder.Property(a => a.UniversidadId).HasMaxLength(450);

        // Pertenece: Area → Universidad (0,1)
        builder.HasOne(a => a.Universidad)
            .WithMany(u => u.Areas)
            .HasForeignKey(a => a.UniversidadId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
