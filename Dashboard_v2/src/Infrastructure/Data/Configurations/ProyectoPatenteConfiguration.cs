using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard_v2.Infrastructure.Data.Configurations;

public class ProyectoPatenteConfiguration : IEntityTypeConfiguration<ProyectoPatente>
{
    public void Configure(EntityTypeBuilder<ProyectoPatente> builder)
    {
        builder.ToTable("ProyectoPatentes");

        builder.HasKey(pp => new { pp.ProyectoId, pp.PatenteId });

        builder.Property(pp => pp.ProyectoId).HasMaxLength(450);
        builder.Property(pp => pp.PatenteId).HasMaxLength(450);

        builder.HasOne(pp => pp.Proyecto)
            .WithMany(p => p.PatentesDerivadas)
            .HasForeignKey(pp => pp.ProyectoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pp => pp.Patente)
            .WithMany(p => p.ProyectosDerivados)
            .HasForeignKey(pp => pp.PatenteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
