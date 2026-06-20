using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard_v2.Infrastructure.Data.Configurations;

public class BaseDeDatosPublicacionConfiguration : IEntityTypeConfiguration<BaseDeDatosPublicacion>
{
    public void Configure(EntityTypeBuilder<BaseDeDatosPublicacion> builder)
    {
        builder.ToTable("BasesDeDatosPublicacion");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Nombre).IsRequired().HasMaxLength(500);
    }
}
