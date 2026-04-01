using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard_v2.Infrastructure.Data.Configurations;

public class JournalPublicationConfiguration : IEntityTypeConfiguration<JournalPublication>
{
    public void Configure(EntityTypeBuilder<JournalPublication> builder)
    {
        builder.ToTable("JournalPublications");

        builder.HasKey(jp => jp.PublicationId);
        builder.Property(jp => jp.PublicationId).HasMaxLength(450);
        builder.Property(jp => jp.Name).IsRequired().HasMaxLength(500);
        builder.Property(jp => jp.DataBase).IsRequired().HasMaxLength(500);
        builder.Property(jp => jp.Group).IsRequired();

        builder.HasOne(jp => jp.Publication)
            .WithOne(p => p.JournalPublication)
            .HasForeignKey<JournalPublication>(jp => jp.PublicationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
