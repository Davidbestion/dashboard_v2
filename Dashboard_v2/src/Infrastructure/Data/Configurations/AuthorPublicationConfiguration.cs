using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard_v2.Infrastructure.Data.Configurations;

public class AuthorPublicationConfiguration : IEntityTypeConfiguration<AuthorPublication>
{
    public void Configure(EntityTypeBuilder<AuthorPublication> builder)
    {
        builder.ToTable("AuthorPublications");

        // Clave compuesta: un autor solo puede aparecer una vez por publicación
        builder.HasKey(ap => new { ap.AuthorId, ap.PublicationId });

        builder.Property(ap => ap.AuthorId).HasMaxLength(450);
        builder.Property(ap => ap.PublicationId).HasMaxLength(450);

        builder.HasOne(ap => ap.Author)
            .WithMany(a => a.AuthorPublications)
            .HasForeignKey(ap => ap.AuthorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ap => ap.Publication)
            .WithMany(p => p.AuthorPublications)
            .HasForeignKey(ap => ap.PublicationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
