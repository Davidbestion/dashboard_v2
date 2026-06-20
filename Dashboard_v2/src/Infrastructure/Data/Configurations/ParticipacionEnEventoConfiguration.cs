using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard_v2.Infrastructure.Data.Configurations;

public class ParticipacionEnEventoConfiguration : IEntityTypeConfiguration<ParticipacionEnEvento>
{
    public void Configure(EntityTypeBuilder<ParticipacionEnEvento> builder)
    {
        builder.ToTable("ParticipacionesEnEvento");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.UserId).IsRequired().HasMaxLength(450);
        builder.Property(p => p.Fecha).IsRequired();

        builder.HasOne(p => p.User)
            .WithMany(u => u.ParticipacionesEnEventos)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Event)
            .WithMany(e => e.Participaciones)
            .HasForeignKey(p => p.EventId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
