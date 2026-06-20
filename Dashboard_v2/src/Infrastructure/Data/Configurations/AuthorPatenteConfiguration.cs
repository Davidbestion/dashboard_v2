using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard_v2.Infrastructure.Data.Configurations;

public class AuthorPatenteConfiguration : IEntityTypeConfiguration<AuthorPatente>
{
    public void Configure(EntityTypeBuilder<AuthorPatente> builder)
    {
        builder.ToTable("AuthorPatentes");

        builder.HasKey(ap => new { ap.AuthorId, ap.PatenteId });

        builder.Property(ap => ap.AuthorId).HasMaxLength(450);
        builder.Property(ap => ap.PatenteId).HasMaxLength(450);

        builder.HasOne(ap => ap.Author)
            .WithMany(a => a.AuthorPatentes)
            .HasForeignKey(ap => ap.AuthorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ap => ap.Patente)
            .WithMany(p => p.Creadores)
            .HasForeignKey(ap => ap.PatenteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
