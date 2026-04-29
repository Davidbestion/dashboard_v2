using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard_v2.Infrastructure.Data.Configurations;

public class RedCoordinadaConfiguration : IEntityTypeConfiguration<RedCoordinada>
{
    public void Configure(EntityTypeBuilder<RedCoordinada> builder)
    {
        builder.ToTable("RedesCoordinadas");

        builder.HasKey(rc => rc.Id);
        builder.Property(rc => rc.Id).HasMaxLength(450);

        builder.Property(rc => rc.RedId).IsRequired().HasMaxLength(450);
        builder.Property(rc => rc.AreaId).IsRequired().HasMaxLength(450);
        builder.Property(rc => rc.CoordinadorId).IsRequired().HasMaxLength(450);

        builder.HasOne(rc => rc.Red)
            .WithMany(r => r.RedesCoordinadas)
            .HasForeignKey(rc => rc.RedId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rc => rc.Area)
            .WithMany(a => a.RedesCoordinadas)
            .HasForeignKey(rc => rc.AreaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rc => rc.Coordinador)
            .WithMany(u => u.RedesCoordinadas)
            .HasForeignKey(rc => rc.CoordinadorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(rc => new { rc.RedId, rc.AreaId }).IsUnique();
    }
}
