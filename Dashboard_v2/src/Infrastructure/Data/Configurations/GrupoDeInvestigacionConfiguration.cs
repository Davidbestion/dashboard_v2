using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard_v2.Infrastructure.Data.Configurations;

public class GrupoDeInvestigacionConfiguration : IEntityTypeConfiguration<GrupoDeInvestigacion>
{
    public void Configure(EntityTypeBuilder<GrupoDeInvestigacion> builder)
    {
        builder.ToTable("GruposDeInvestigacion");

        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).HasMaxLength(450);
        builder.Property(g => g.Nombre).IsRequired().HasMaxLength(500);
        builder.Property(g => g.AreaId).IsRequired().HasMaxLength(450);
        builder.Property(g => g.CreadorId).HasMaxLength(450);

        // Creador (nullable FK a User)
        builder.HasOne(g => g.Creador)
            .WithMany()
            .HasForeignKey(g => g.CreadorId)
            .OnDelete(DeleteBehavior.SetNull);

        // Posee: GrupoDeInvestigacion → Area (1,1)
        builder.HasOne(g => g.Area)
            .WithMany(a => a.GruposDeInvestigacion)
            .HasForeignKey(g => g.AreaId)
            .OnDelete(DeleteBehavior.Cascade);

        // Estudia: GrupoDeInvestigacion ↔ LineaDeInvestigacion (N:N)
        builder.HasMany(g => g.LineasDeInvestigacion)
            .WithMany(l => l.GruposDeInvestigacion)
            .UsingEntity<Dictionary<string, object>>(
                "GrupoDeInvestigacionLineaDeInvestigacion",
                right => right
                    .HasOne<LineaDeInvestigacion>()
                    .WithMany()
                    .HasForeignKey("LineaDeInvestigacionId")
                    .HasPrincipalKey(l => l.Id)
                    .OnDelete(DeleteBehavior.Cascade),
                left => left
                    .HasOne<GrupoDeInvestigacion>()
                    .WithMany()
                    .HasForeignKey("GrupoDeInvestigacionId")
                    .HasPrincipalKey(g => g.Id)
                    .OnDelete(DeleteBehavior.Cascade),
                join =>
                {
                    join.ToTable("GruposDeInvestigacionLineasDeInvestigacion");
                    join.HasKey("GrupoDeInvestigacionId", "LineaDeInvestigacionId");
                });

        // Miembros: GrupoDeInvestigacion ↔ User (N:N)
        builder.HasMany(g => g.Usuarios)
            .WithMany(u => u.GruposDeInvestigacion)
            .UsingEntity<Dictionary<string, object>>(
                "GrupoDeInvestigacionUsuario",
                right => right
                    .HasOne<User>()
                    .WithMany()
                    .HasForeignKey("UsuarioId")
                    .HasPrincipalKey(u => u.Id)
                    .OnDelete(DeleteBehavior.Cascade),
                left => left
                    .HasOne<GrupoDeInvestigacion>()
                    .WithMany()
                    .HasForeignKey("GrupoDeInvestigacionId")
                    .HasPrincipalKey(g => g.Id)
                    .OnDelete(DeleteBehavior.Cascade),
                join =>
                {
                    join.ToTable("GruposDeInvestigacionUsuarios");
                    join.HasKey("GrupoDeInvestigacionId", "UsuarioId");
                });
    }
}
