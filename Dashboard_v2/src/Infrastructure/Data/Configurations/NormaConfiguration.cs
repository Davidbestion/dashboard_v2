using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard_v2.Infrastructure.Data.Configurations;

public class NormaConfiguration : IEntityTypeConfiguration<Norma>
{
    public void Configure(EntityTypeBuilder<Norma> builder)
    {
        builder.ToTable("Normas");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).HasMaxLength(450);
        builder.Property(n => n.Titulo).IsRequired().HasMaxLength(1000);

        builder.Property(n => n.TipoNormaId).IsRequired(false);
        builder.HasOne(n => n.TipoNorma)
            .WithMany()
            .HasForeignKey(n => n.TipoNormaId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasOne(n => n.Institution)
            .WithMany(i => i.Normas)
            .HasForeignKey(n => n.InstitutionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
