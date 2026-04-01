using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard_v2.Infrastructure.Data.Configurations;

public class JournalGroup1PublicationConfiguration : IEntityTypeConfiguration<JournalGroup1Publication>
{
    public void Configure(EntityTypeBuilder<JournalGroup1Publication> builder)
    {
        builder.ToTable("JournalGroup1Publications");

        builder.HasKey(jg => jg.PublicationId);
        builder.Property(jg => jg.PublicationId).HasMaxLength(450);
        builder.Property(jg => jg.Cuartil).IsRequired();

        builder.HasOne(jg => jg.JournalPublication)
            .WithOne(jp => jp.JournalGroup1Publication)
            .HasForeignKey<JournalGroup1Publication>(jg => jg.PublicationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
