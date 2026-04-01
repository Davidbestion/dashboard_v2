using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard_v2.Infrastructure.Data.Configurations;

public class IndexedPublicationConfiguration : IEntityTypeConfiguration<IndexedPublication>
{
    public void Configure(EntityTypeBuilder<IndexedPublication> builder)
    {
        builder.ToTable("IndexedPublications");

        builder.HasKey(ip => ip.PublicationId);
        builder.Property(ip => ip.PublicationId).HasMaxLength(450);
        builder.Property(ip => ip.Index).IsRequired();

        builder.HasOne(ip => ip.Publication)
            .WithOne(p => p.IndexedPublication)
            .HasForeignKey<IndexedPublication>(ip => ip.PublicationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
