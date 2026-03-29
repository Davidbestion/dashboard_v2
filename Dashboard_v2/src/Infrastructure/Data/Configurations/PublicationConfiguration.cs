using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard_v2.Infrastructure.Data.Configurations;

public class PublicationConfiguration : IEntityTypeConfiguration<Publication>
{
    public void Configure(EntityTypeBuilder<Publication> builder)
    {
        builder.ToTable("Publications");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasMaxLength(450);
        builder.Property(p => p.Title).IsRequired().HasMaxLength(500);
        builder.Property(p => p.PublicationData).IsRequired();
        builder.Property(p => p.PublicationTypeId).IsRequired().HasMaxLength(450);

        // Una publicación tiene exactamente un tipo (no se borra el tipo si hay publicaciones)
        builder.HasOne(p => p.PublicationType)
            .WithMany()
            .HasForeignKey(p => p.PublicationTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
