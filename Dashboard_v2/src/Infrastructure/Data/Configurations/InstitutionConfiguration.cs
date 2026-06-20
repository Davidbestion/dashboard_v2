using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard_v2.Infrastructure.Data.Configurations;

public class InstitutionConfiguration : IEntityTypeConfiguration<Institution>
{
    public void Configure(EntityTypeBuilder<Institution> builder)
    {
        builder.ToTable("Institutions");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasMaxLength(450);
        builder.Property(i => i.Nombre).IsRequired().HasMaxLength(500);
    }
}
