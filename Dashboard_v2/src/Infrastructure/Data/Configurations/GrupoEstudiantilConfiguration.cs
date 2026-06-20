using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard_v2.Infrastructure.Data.Configurations;

public class GrupoEstudiantilConfiguration : IEntityTypeConfiguration<GrupoEstudiantil>
{
    public void Configure(EntityTypeBuilder<GrupoEstudiantil> builder)
    {
        builder.ToTable("GruposEstudiantiles");

        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).HasMaxLength(450);
        builder.Property(g => g.Nombre).IsRequired().HasMaxLength(500);
        builder.Property(g => g.AreaId).IsRequired().HasMaxLength(450);

        // Relación con Area (sin navegación inversa específica)
        builder.HasOne(g => g.Area)
            .WithMany()
            .HasForeignKey(g => g.AreaId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relación N:N con LineasDeInvestigacion
        builder.HasMany(g => g.LineasDeInvestigacion)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "GrupoEstudiantilLineaDeInvestigacion",
                right => right
                    .HasOne<LineaDeInvestigacion>()
                    .WithMany()
                    .HasForeignKey("LineaDeInvestigacionId")
                    .HasPrincipalKey(l => l.Id)
                    .OnDelete(DeleteBehavior.Cascade),
                left => left
                    .HasOne<GrupoEstudiantil>()
                    .WithMany()
                    .HasForeignKey("GrupoEstudiantilId")
                    .HasPrincipalKey(g => g.Id)
                    .OnDelete(DeleteBehavior.Cascade),
                join =>
                {
                    join.ToTable("GruposEstudiantilesLineasDeInvestigacion");
                    join.HasKey("GrupoEstudiantilId", "LineaDeInvestigacionId");
                });
    }
}
