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
    public DbSet<EventInstitution> EventInstitutions => Set<EventInstitution>();
    public DbSet<EventAreaPatrocinio> EventAreasPatrocinio => Set<EventAreaPatrocinio>();
    public DbSet<Presentation> Presentations => Set<Presentation>();
    public DbSet<AuthorPresentation> AuthorPresentations => Set<AuthorPresentation>();
    public DbSet<IndexedPublication> IndexedPublications => Set<IndexedPublication>();
    public DbSet<JournalPublication> JournalPublications => Set<JournalPublication>();
    public DbSet<JournalGroup1Publication> JournalGroup1Publications => Set<JournalGroup1Publication>();
    public DbSet<Universidad> Universidades => Set<Universidad>();
    public DbSet<Institution> Institutions => Set<Institution>();
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<GrupoDeInvestigacion> GruposDeInvestigacion => Set<GrupoDeInvestigacion>();
    public DbSet<GrupoEstudiantil> GruposEstudiantiles => Set<GrupoEstudiantil>();
    public DbSet<LineaDeInvestigacion> LineasDeInvestigacion => Set<LineaDeInvestigacion>();
    public DbSet<AreaDelConocimiento> AreasDelConocimiento => Set<AreaDelConocimiento>();
    public DbSet<Clasificacion> Clasificaciones => Set<Clasificacion>();
    public DbSet<Proyecto> Proyectos => Set<Proyecto>();
    public DbSet<Registro> Registros => Set<Registro>();
    public DbSet<Norma> Normas => Set<Norma>();
    public DbSet<Red> Reds => Set<Red>();
    public DbSet<RedCoordinada> RedesCoordinadas => Set<RedCoordinada>();
    public DbSet<TipoProductoComercializado> TipoProductosComercializados => Set<TipoProductoComercializado>();
    public DbSet<ProductoComercializado> ProductosComercializados => Set<ProductoComercializado>();
    public DbSet<Patente> Patentes => Set<Patente>();
    public DbSet<StoredFile> StoredFiles => Set<StoredFile>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
