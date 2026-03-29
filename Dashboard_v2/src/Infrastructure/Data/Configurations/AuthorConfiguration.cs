using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard_v2.Infrastructure.Data.Configurations;

public class AuthorConfiguration : IEntityTypeConfiguration<Author>
{
    public void Configure(EntityTypeBuilder<Author> builder)
    {
        builder.ToTable("Authors");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasMaxLength(450);
        builder.Property(a => a.Name).IsRequired().HasMaxLength(500);
        builder.Property(a => a.UserId).HasMaxLength(450);

        // Un usuario registrado puede tener como máximo un perfil de autor.
        // El índice único sobre UserId garantiza eso; el filtro excluye los nulls
        // para permitir múltiples autores sin cuenta.
        builder.HasIndex(a => a.UserId)
            .IsUnique()
            .HasFilter("\"UserId\" IS NOT NULL")
            .HasDatabaseName("IX_Authors_UserId_Unique");

        // Relación 1-a-1 opcional con User
        builder.HasOne(a => a.User)
            .WithOne(u => u.AuthorProfile)
            .HasForeignKey<Author>(a => a.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
