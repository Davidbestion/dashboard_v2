using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard_v2.Infrastructure.Data.Configurations;

public class UserAwardedConfiguration : IEntityTypeConfiguration<UserAwarded>
{
    public void Configure(EntityTypeBuilder<UserAwarded> builder)
    {
        builder.ToTable("UserAwardeds");

        builder.HasKey(ua => ua.Id);

        builder.HasOne(ua => ua.User)
            .WithMany()
            .HasForeignKey(ua => ua.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ua => ua.Award)
            .WithMany(a => a.UserAwardees)
            .HasForeignKey(ua => ua.AwardId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ua => ua.EvidenceFile)
            .WithMany()
            .HasForeignKey(ua => ua.EvidenceFileId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
    }
}
