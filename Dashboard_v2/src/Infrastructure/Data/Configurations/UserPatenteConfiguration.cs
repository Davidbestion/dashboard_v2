using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard_v2.Infrastructure.Data.Configurations;

public class UserPatenteConfiguration : IEntityTypeConfiguration<UserPatente>
{
    public void Configure(EntityTypeBuilder<UserPatente> builder)
    {
        builder.ToTable("UserPatentes");

        builder.HasKey(up => new { up.UserId, up.PatenteId });

        builder.Property(up => up.UserId).HasMaxLength(450);
        builder.Property(up => up.PatenteId).HasMaxLength(450);

        builder.HasOne(up => up.User)
            .WithMany(u => u.PatentesCreadas)
            .HasForeignKey(up => up.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(up => up.Patente)
            .WithMany()
            .HasForeignKey(up => up.PatenteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
