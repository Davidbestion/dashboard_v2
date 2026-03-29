using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard_v2.Infrastructure.Data.Configurations;

public class PublicationTypeConfiguration : IEntityTypeConfiguration<PublicationType>
{
    public void Configure(EntityTypeBuilder<PublicationType> builder)
    {
        builder.ToTable("PublicationTypes");

        builder.HasKey(pt => pt.Id);
        builder.Property(pt => pt.Id).HasMaxLength(450);
        builder.Property(pt => pt.Name).IsRequired().HasMaxLength(200);

        builder.HasIndex(pt => pt.Name).IsUnique().HasDatabaseName("IX_PublicationTypes_Name");
    }
}
