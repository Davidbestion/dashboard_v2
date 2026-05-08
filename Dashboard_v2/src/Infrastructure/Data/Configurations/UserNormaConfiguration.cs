using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard_v2.Infrastructure.Data.Configurations;

public class UserNormaConfiguration : IEntityTypeConfiguration<UserNorma>
{
    public void Configure(EntityTypeBuilder<UserNorma> builder)
    {
        builder.ToTable("UserNormas");

        builder.HasKey(un => new { un.UserId, un.NormaId });

        builder.Property(un => un.UserId).HasMaxLength(450);
        builder.Property(un => un.NormaId).HasMaxLength(450);

        builder.HasOne(un => un.User)
            .WithMany(u => u.NormasCreadas)
            .HasForeignKey(un => un.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(un => un.Norma)
            .WithMany(n => n.Creadores)
            .HasForeignKey(un => un.NormaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
