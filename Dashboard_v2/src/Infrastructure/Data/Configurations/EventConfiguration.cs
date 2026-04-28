using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dashboard_v2.Infrastructure.Data.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("Events");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).IsRequired().HasMaxLength(500);

        // Relación N:N con Institutions (evento organiza 1..N instituciones)
        builder.HasMany(e => e.Institutions)
            .WithMany(i => i.Events)
            .UsingEntity<Dictionary<string, object>>(
                "EventInstitution",
                right => right
                    .HasOne<Institution>()
                    .WithMany()
                    .HasForeignKey("InstitutionId")
                    .HasPrincipalKey(i => i.Id)
                    .OnDelete(DeleteBehavior.Cascade),
                left => left
                    .HasOne<Event>()
                    .WithMany()
                    .HasForeignKey("EventId")
                    .HasPrincipalKey(e => e.Id)
                    .OnDelete(DeleteBehavior.Cascade),
                join =>
                {
                    join.ToTable("EventsInstitutions");
                    join.HasKey("EventId", "InstitutionId");
                    join.Property<string>("InstitutionId").HasMaxLength(450);
                });

        builder.HasOne(e => e.Country)
            .WithMany(c => c.Events)
            .HasForeignKey(e => e.CountryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.EventType)
            .WithMany(t => t.Events)
            .HasForeignKey(e => e.EventTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relación opcional con Red (una Red coordina muchos Eventos; un Evento puede tener 0..1 Red)
        builder.HasOne(e => e.Red)
            .WithMany(r => r.Events)
            .HasForeignKey(e => e.RedId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
