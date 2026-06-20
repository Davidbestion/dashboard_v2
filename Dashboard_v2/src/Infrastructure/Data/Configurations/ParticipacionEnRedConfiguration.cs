using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard_v2.Infrastructure.Data.Configurations;

public class ParticipacionEnRedConfiguration : IEntityTypeConfiguration<ParticipacionEnRed>
{
    public void Configure(EntityTypeBuilder<ParticipacionEnRed> builder)
    {
        builder.ToTable("ParticipacionesEnRed");

        builder.HasKey(p => new { p.RedId, p.AuthorId });
        builder.Property(p => p.RedId).HasMaxLength(450);
        builder.Property(p => p.AuthorId).HasMaxLength(450);

        builder.HasOne(p => p.Red)
            .WithMany(r => r.Participaciones)
            .HasForeignKey(p => p.RedId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Author)
            .WithMany(a => a.ParticipacionesEnRedes)
            .HasForeignKey(p => p.AuthorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
