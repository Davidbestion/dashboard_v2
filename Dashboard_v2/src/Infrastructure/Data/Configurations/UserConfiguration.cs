using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard_v2.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasMaxLength(450);
        builder.Property(u => u.UserName).IsRequired().HasMaxLength(256);
        builder.Property(u => u.UserLastName1).IsRequired().HasMaxLength(256);
        builder.Property(u => u.UserLastName2).HasMaxLength(256);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
        builder.Property(u => u.PasswordHash);
        builder.Property(u => u.BirthDate);
        builder.Property(u => u.IsTrained).HasDefaultValue(false);
        builder.Property(u => u.IsActive).HasDefaultValue(true);

        builder.HasIndex(u => u.UserName).IsUnique().HasDatabaseName("UserNameIndex");
        builder.HasIndex(u => u.Email).IsUnique().HasDatabaseName("EmailIndex");
    }
}
