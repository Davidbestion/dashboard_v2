using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard_v2.Infrastructure.Data.Configurations;

public class RedConfiguration : IEntityTypeConfiguration<Red>
{
    public void Configure(EntityTypeBuilder<Red> builder)
    {
        builder.ToTable("Reds");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasMaxLength(450);
        builder.Property(r => r.Nombre).IsRequired().HasMaxLength(500);
        builder.Property(r => r.CantidadProfesores);
        builder.Property(r => r.CoordinadorId).HasMaxLength(450);

        builder.HasOne(r => r.Country)
            .WithMany(c => c.Reds)
            .HasForeignKey(r => r.CountryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Coordinador)
            .WithMany(u => u.RedesCoordinadas)
            .HasForeignKey(r => r.CoordinadorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(r => r.Events)
            .WithOne(e => e.Red)
            .HasForeignKey(e => e.RedId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
