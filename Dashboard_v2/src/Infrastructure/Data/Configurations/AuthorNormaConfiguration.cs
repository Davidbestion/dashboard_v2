using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard_v2.Infrastructure.Data.Configurations;

public class AuthorNormaConfiguration : IEntityTypeConfiguration<AuthorNorma>
{
    public void Configure(EntityTypeBuilder<AuthorNorma> builder)
    {
        builder.ToTable("AuthorNormas");

        builder.HasKey(an => new { an.AuthorId, an.NormaId });

        builder.Property(an => an.AuthorId).HasMaxLength(450);
        builder.Property(an => an.NormaId).HasMaxLength(450);

        builder.HasOne(an => an.Author)
            .WithMany(a => a.AuthorNormas)
            .HasForeignKey(an => an.AuthorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(an => an.Norma)
            .WithMany(n => n.Creadores)
            .HasForeignKey(an => an.NormaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
