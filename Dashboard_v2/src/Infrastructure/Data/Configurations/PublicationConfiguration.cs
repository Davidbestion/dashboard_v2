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

        builder.Property(p => p.PublicationType).IsRequired();

        builder.Property(p => p.ProyectoId).HasMaxLength(450);

        // Una publicación puede pertenecer a un proyecto (opcional).
        // Si el proyecto se borra, la publicación queda desvinculada (SetNull).
        builder.HasOne(p => p.Proyecto)
            .WithMany(pr => pr.PublicacionesDerivadas)
            .HasForeignKey(p => p.ProyectoId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
    }
}
