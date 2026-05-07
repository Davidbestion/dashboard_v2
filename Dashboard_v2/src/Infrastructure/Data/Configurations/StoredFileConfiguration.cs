using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard_v2.Infrastructure.Data.Configurations;

public class StoredFileConfiguration : IEntityTypeConfiguration<StoredFile>
{
    public void Configure(EntityTypeBuilder<StoredFile> builder)
    {
        builder.ToTable("StoredFiles");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.FileName)
            .IsRequired()
            .HasMaxLength(260);

        builder.Property(f => f.ObjectKey)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(f => f.BucketName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(f => f.ContentType)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(f => f.SizeBytes)
            .IsRequired();

        builder.Property(f => f.UploadedById)
            .IsRequired()
            .HasMaxLength(450);

        builder.HasIndex(f => f.ObjectKey).IsUnique();
        builder.HasIndex(f => f.UploadedById);

        builder.HasOne(f => f.UploadedBy)
            .WithMany()
            .HasForeignKey(f => f.UploadedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
