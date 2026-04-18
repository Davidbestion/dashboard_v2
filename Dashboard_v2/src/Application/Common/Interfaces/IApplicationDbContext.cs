using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dashboard_v2.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<Resource> Resources { get; }
    DbSet<Publication> Publications { get; }
    DbSet<Author> Authors { get; }
    DbSet<AuthorPublication> AuthorPublications { get; }
    DbSet<Award> Awards { get; }
    DbSet<AwardType> AwardTypes { get; }
    DbSet<UserAwarded> UserAwardeds { get; }
    DbSet<Country> Countries { get; }
    DbSet<Event> Events { get; }
    DbSet<EventType> EventTypes { get; }
    DbSet<Presentation> Presentations { get; }
    DbSet<AuthorPresentation> AuthorPresentations { get; }
    DbSet<IndexedPublication> IndexedPublications { get; }
    DbSet<JournalPublication> JournalPublications { get; }
    DbSet<JournalGroup1Publication> JournalGroup1Publications { get; }
    DbSet<Universidad> Universidades { get; }
    DbSet<Area> Areas { get; }
    DbSet<GrupoDeInvestigacion> GruposDeInvestigacion { get; }
    DbSet<LineaDeInvestigacion> LineasDeInvestigacion { get; }
    DbSet<AreaDelConocimiento> AreasDelConocimiento { get; }
    DbSet<Clasificacion> Clasificaciones { get; }
    DbSet<Proyecto> Proyectos { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
