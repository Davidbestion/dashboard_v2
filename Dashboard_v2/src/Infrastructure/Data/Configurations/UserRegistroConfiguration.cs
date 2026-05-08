using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard_v2.Infrastructure.Data.Configurations;

public class UserRegistroConfiguration : IEntityTypeConfiguration<UserRegistro>
{
    public void Configure(EntityTypeBuilder<UserRegistro> builder)
    {
        builder.ToTable("UserRegistros");

        builder.HasKey(ur => new { ur.UserId, ur.RegistroId });

        builder.Property(ur => ur.UserId).HasMaxLength(450);
        builder.Property(ur => ur.RegistroId).HasMaxLength(450);

        builder.HasOne(ur => ur.User)
            .WithMany(u => u.RegistrosCreados)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ur => ur.Registro)
            .WithMany(r => r.Creadores)
            .HasForeignKey(ur => ur.RegistroId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
