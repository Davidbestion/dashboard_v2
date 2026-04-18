using System.Reflection;
using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dashboard_v2.Infrastructure.Data;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Resource> Resources => Set<Resource>();
    public DbSet<Publication> Publications => Set<Publication>();
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<AuthorPublication> AuthorPublications => Set<AuthorPublication>();
    public DbSet<Award> Awards => Set<Award>();
    public DbSet<AwardType> AwardTypes => Set<AwardType>();
    public DbSet<UserAwarded> UserAwardeds => Set<UserAwarded>();
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventType> EventTypes => Set<EventType>();
    public DbSet<Presentation> Presentations => Set<Presentation>();
    public DbSet<AuthorPresentation> AuthorPresentations => Set<AuthorPresentation>();
    public DbSet<IndexedPublication> IndexedPublications => Set<IndexedPublication>();
    public DbSet<JournalPublication> JournalPublications => Set<JournalPublication>();
    public DbSet<JournalGroup1Publication> JournalGroup1Publications => Set<JournalGroup1Publication>();
    public DbSet<Universidad> Universidades => Set<Universidad>();
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<GrupoDeInvestigacion> GruposDeInvestigacion => Set<GrupoDeInvestigacion>();
    public DbSet<LineaDeInvestigacion> LineasDeInvestigacion => Set<LineaDeInvestigacion>();
    public DbSet<AreaDelConocimiento> AreasDelConocimiento => Set<AreaDelConocimiento>();
    public DbSet<Clasificacion> Clasificaciones => Set<Clasificacion>();
    public DbSet<Proyecto> Proyectos => Set<Proyecto>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
