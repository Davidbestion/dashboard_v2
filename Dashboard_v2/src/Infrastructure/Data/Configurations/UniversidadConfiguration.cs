using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard_v2.Infrastructure.Data.Configurations;

public class UniversidadConfiguration : IEntityTypeConfiguration<Universidad>
{
    public void Configure(EntityTypeBuilder<Universidad> builder)
    {
        builder.ToTable("Universidades");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasMaxLength(450);
        builder.Property(u => u.Nombre).IsRequired().HasMaxLength(500);
    }
}
