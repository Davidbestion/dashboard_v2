using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard_v2.Infrastructure.Data.Configurations;

public class AuthorRegistroConfiguration : IEntityTypeConfiguration<AuthorRegistro>
{
    public void Configure(EntityTypeBuilder<AuthorRegistro> builder)
    {
        builder.ToTable("AuthorRegistros");

        builder.HasKey(ar => new { ar.AuthorId, ar.RegistroId });

        builder.Property(ar => ar.AuthorId).HasMaxLength(450);
        builder.Property(ar => ar.RegistroId).HasMaxLength(450);

        builder.HasOne(ar => ar.Author)
            .WithMany(a => a.AuthorRegistros)
            .HasForeignKey(ar => ar.AuthorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ar => ar.Registro)
            .WithMany(r => r.Creadores)
            .HasForeignKey(ar => ar.RegistroId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
