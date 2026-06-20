using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard_v2.Infrastructure.Data.Configurations;

public class EventOrganizadorConfiguration : IEntityTypeConfiguration<EventOrganizador>
{
    public void Configure(EntityTypeBuilder<EventOrganizador> builder)
    {
        builder.ToTable("EventOrganizadores");
        builder.HasKey(eo => new { eo.EventId, eo.UserId });
        builder.Property(eo => eo.UserId).HasMaxLength(450);

        builder.HasOne(eo => eo.Event)
            .WithMany(e => e.Organizadores)
            .HasForeignKey(eo => eo.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(eo => eo.User)
            .WithMany(u => u.EventosOrganizados)
            .HasForeignKey(eo => eo.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
