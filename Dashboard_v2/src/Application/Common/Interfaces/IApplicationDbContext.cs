using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dashboard_v2.Application.Common.Interfaces;

/// <summary>
/// Abstraction over EF Core DbContext exposing DbSets and SaveChanges. Allows application services to query and persist entities without depending on the concrete DbContext.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<Publication> Publications { get; }
    DbSet<Author> Authors { get; }
    DbSet<AuthorPublication> AuthorPublications { get; }
    DbSet<Award> Awards { get; }
    DbSet<AwardType> AwardTypes { get; }
    DbSet<UserAwarded> UserAwardees { get; }
    DbSet<Country> Countries { get; }
    DbSet<Event> Events { get; }
    DbSet<EventType> EventTypes { get; }
    DbSet<EventInstitution> EventInstitutions { get; }
    DbSet<EventOrganizador> EventOrganizadores { get; }
    DbSet<ParticipacionEnEvento> ParticipacionesEnEventos { get; }
    DbSet<Presentation> Presentations { get; }
    DbSet<IndexedPublication> IndexedPublications { get; }
    DbSet<JournalPublication> JournalPublications { get; }
    DbSet<JournalGroup1Publication> JournalGroup1Publications { get; }
    DbSet<BaseDeDatosPublicacion> BasesDeDatosPublicacion { get; }
    DbSet<Universidad> Universidades { get; }
    DbSet<Institution> Institutions { get; }
    DbSet<Area> Areas { get; }
    DbSet<GrupoDeInvestigacion> GruposDeInvestigacion { get; }
    DbSet<GrupoEstudiantil> GruposEstudiantiles { get; }
    DbSet<LineaDeInvestigacion> LineasDeInvestigacion { get; }
    DbSet<AreaDelConocimiento> AreasDelConocimiento { get; }
    DbSet<Clasificacion> Clasificaciones { get; }
    DbSet<Proyecto> Proyectos { get; }
    DbSet<Registro> Registros { get; }
    DbSet<Norma> Normas { get; }
    DbSet<TipoNorma> TiposNorma { get; }
    DbSet<Red> Reds { get; }
    DbSet<ParticipacionEnRed> ParticipacionesEnRed { get; }
    DbSet<TipoProductoComercializado> TipoProductosComercializados { get; }
    DbSet<ProductoComercializado> ProductosComercializados { get; }
    DbSet<Patente> Patentes { get; }
    DbSet<StoredFile> StoredFiles { get; }
    DbSet<AuthorRegistro> AuthorRegistros { get; }
    DbSet<AuthorNorma> AuthorNormas { get; }
    DbSet<AuthorProductoComercializado> AuthorProductosComercializados { get; }
    DbSet<AuthorPatente> AuthorPatentes { get; }
    DbSet<ProyectoPatente> ProyectoPatentes { get; }
    DbSet<Provincia> Provincias { get; }
    DbSet<Municipio> Municipios { get; }
    DbSet<SectorEstrategico> SectoresEstrategicos { get; }
    DbSet<EjeEstrategico> EjesEstrategicos { get; }
    DbSet<FuenteFinanciacion> FuentesFinanciacion { get; }
    DbSet<EstadoProyecto> EstadosProyecto { get; }
    DbSet<SituacionProyecto> SituacionesProyecto { get; }
    DbSet<Programa> Programas { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
