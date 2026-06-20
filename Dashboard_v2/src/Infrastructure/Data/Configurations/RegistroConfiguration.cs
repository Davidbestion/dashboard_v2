using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard_v2.Infrastructure.Data.Configurations;

public class RegistroConfiguration : IEntityTypeConfiguration<Registro>
{
    public void Configure(EntityTypeBuilder<Registro> builder)
    {
        builder.ToTable("Registros");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasMaxLength(450);
        builder.Property(r => r.Titulo).IsRequired().HasMaxLength(1000);
        builder.Property(r => r.NumeroCertificado).HasMaxLength(500);
        builder.Property(r => r.EsInformatico).IsRequired().HasDefaultValue(false);

        builder.HasOne(r => r.Country)
            .WithMany(c => c.Registros)
            .HasForeignKey(r => r.CountryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Institution)
            .WithMany(i => i.Registros)
            .HasForeignKey(r => r.InstitutionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.EvidenceFile)
            .WithMany()
            .HasForeignKey(r => r.EvidenceFileId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
    }
}
