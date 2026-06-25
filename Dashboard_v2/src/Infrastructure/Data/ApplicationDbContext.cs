using System.Reflection;
using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dashboard_v2.Infrastructure.Data;

/// <summary>
/// EF Core DbContext for the research management system. Implements <see cref="IApplicationDbContext"/>
/// to allow application services to depend on abstraction.
/// </summary>
public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    /// <summary>Application users registered in the system.</summary>
    public DbSet<User> Users => Set<User>();
    /// <summary>Role assignments linking users to their access roles.</summary>
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    /// <summary>Scientific publications authored by members of the institution.</summary>
    public DbSet<Publication> Publications => Set<Publication>();
    /// <summary>Authors associated with publications.</summary>
    public DbSet<Author> Authors => Set<Author>();
    /// <summary>Join table linking authors to publications.</summary>
    public DbSet<AuthorPublication> AuthorPublications => Set<AuthorPublication>();
    /// <summary>Awards received by researchers at the institution.</summary>
    public DbSet<Award> Awards => Set<Award>();
    /// <summary>Reference catalogue of award types (e.g., Academia de Ciencias, MES).</summary>
    public DbSet<AwardType> AwardTypes => Set<AwardType>();
    /// <summary>Records linking users to the awards they received.</summary>
    public DbSet<UserAwarded> UserAwardees => Set<UserAwarded>();
    /// <summary>Country reference data used across the system.</summary>
    public DbSet<Country> Countries => Set<Country>();
    /// <summary>Scientific events (conferences, workshops, etc.).</summary>
    public DbSet<Event> Events => Set<Event>();
    /// <summary>Reference catalogue of event types (Internacional, Nacional, etc.).</summary>
    public DbSet<EventType> EventTypes => Set<EventType>();
    /// <summary>Institutions that participate in organizing events.</summary>
    public DbSet<EventInstitution> EventInstitutions => Set<EventInstitution>();
    /// <summary>Users that organized an event.</summary>
    public DbSet<EventOrganizador> EventOrganizadores => Set<EventOrganizador>();
    /// <summary>User participation records for events.</summary>
    public DbSet<ParticipacionEnEvento> ParticipacionesEnEventos => Set<ParticipacionEnEvento>();
    /// <summary>Paper or poster presentations at events.</summary>
    public DbSet<Presentation> Presentations => Set<Presentation>();
    /// <summary>Publications indexed in at least one bibliographic database.</summary>
    public DbSet<IndexedPublication> IndexedPublications => Set<IndexedPublication>();
    /// <summary>Publications appearing in a scientific journal.</summary>
    public DbSet<JournalPublication> JournalPublications => Set<JournalPublication>();
    /// <summary>Journal publications classified as Group 1 (SCIE/SSCI/AHCI/Scopus Q1-Q2).</summary>
    public DbSet<JournalGroup1Publication> JournalGroup1Publications => Set<JournalGroup1Publication>();
    /// <summary>Reference catalogue of bibliographic databases (Scopus, WoS, DOAJ, etc.).</summary>
    public DbSet<BaseDeDatosPublicacion> BasesDeDatosPublicacion => Set<BaseDeDatosPublicacion>();
    /// <summary>Universities in the institutional catalogue.</summary>
    public DbSet<Universidad> Universidades => Set<Universidad>();
    /// <summary>Generic institutions (research centers, companies, etc.).</summary>
    public DbSet<Institution> Institutions => Set<Institution>();
    /// <summary>Academic departments or areas within the institution.</summary>
    public DbSet<Area> Areas => Set<Area>();
    /// <summary>Research groups affiliated with an area.</summary>
    public DbSet<GrupoDeInvestigacion> GruposDeInvestigacion => Set<GrupoDeInvestigacion>();
    /// <summary>Student research groups affiliated with an area.</summary>
    public DbSet<GrupoEstudiantil> GruposEstudiantiles => Set<GrupoEstudiantil>();
    /// <summary>Research lines that structure the scientific agenda of an area.</summary>
    public DbSet<LineaDeInvestigacion> LineasDeInvestigacion => Set<LineaDeInvestigacion>();
    /// <summary>Knowledge areas used to classify projects and groups.</summary>
    public DbSet<AreaDelConocimiento> AreasDelConocimiento => Set<AreaDelConocimiento>();
    /// <summary>Project classifications (Básica, Aplicada, Experimental, Innovación).</summary>
    public DbSet<Clasificacion> Clasificaciones => Set<Clasificacion>();
    /// <summary>Research projects (all subtypes, using TPT inheritance).</summary>
    public DbSet<Proyecto> Proyectos => Set<Proyecto>();
    /// <summary>Software or other registerable intellectual property records.</summary>
    public DbSet<Registro> Registros => Set<Registro>();
    /// <summary>Technical standards authored by researchers.</summary>
    public DbSet<Norma> Normas => Set<Norma>();
    /// <summary>Reference catalogue of technical standard types.</summary>
    public DbSet<TipoNorma> TiposNorma => Set<TipoNorma>();
    /// <summary>Scientific networks in which researchers participate.</summary>
    public DbSet<Red> Reds => Set<Red>();
    /// <summary>Researcher participation records in scientific networks.</summary>
    public DbSet<ParticipacionEnRed> ParticipacionesEnRed => Set<ParticipacionEnRed>();
    /// <summary>Reference catalogue of commercialized product types.</summary>
    public DbSet<TipoProductoComercializado> TipoProductosComercializados => Set<TipoProductoComercializado>();
    /// <summary>Products or services commercialized by the institution.</summary>
    public DbSet<ProductoComercializado> ProductosComercializados => Set<ProductoComercializado>();
    /// <summary>Patents obtained by researchers.</summary>
    public DbSet<Patente> Patentes => Set<Patente>();
    /// <summary>Evidence and certificate files uploaded by researchers.</summary>
    public DbSet<StoredFile> StoredFiles => Set<StoredFile>();
    /// <summary>Join table linking authors to registros.</summary>
    public DbSet<AuthorRegistro> AuthorRegistros => Set<AuthorRegistro>();
    /// <summary>Join table linking authors to normas.</summary>
    public DbSet<AuthorNorma> AuthorNormas => Set<AuthorNorma>();
    /// <summary>Join table linking authors to commercialized products.</summary>
    public DbSet<AuthorProductoComercializado> AuthorProductosComercializados => Set<AuthorProductoComercializado>();
    /// <summary>Join table linking authors to patents.</summary>
    public DbSet<AuthorPatente> AuthorPatentes => Set<AuthorPatente>();
    /// <summary>Join table linking projects to patents.</summary>
    public DbSet<ProyectoPatente> ProyectoPatentes => Set<ProyectoPatente>();
    /// <summary>Cuban provinces used in project localization data.</summary>
    public DbSet<Provincia> Provincias => Set<Provincia>();
    /// <summary>Cuban municipalities linked to provinces.</summary>
    public DbSet<Municipio> Municipios => Set<Municipio>();
    /// <summary>Strategic sectors used to classify projects.</summary>
    public DbSet<SectorEstrategico> SectoresEstrategicos => Set<SectorEstrategico>();
    /// <summary>Strategic axes used to classify projects.</summary>
    public DbSet<EjeEstrategico> EjesEstrategicos => Set<EjeEstrategico>();
    /// <summary>Funding sources for international collaboration and PNAP projects.</summary>
    public DbSet<FuenteFinanciacion> FuentesFinanciacion => Set<FuenteFinanciacion>();
    /// <summary>Execution states for active projects (En ejecución, Terminado, etc.).</summary>
    public DbSet<EstadoProyecto> EstadosProyecto => Set<EstadoProyecto>();
    /// <summary>Situational states for projects under review.</summary>
    public DbSet<SituacionProyecto> SituacionesProyecto => Set<SituacionProyecto>();
    /// <summary>Research programs that projects may be attached to.</summary>
    public DbSet<Programa> Programas => Set<Programa>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
