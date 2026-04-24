using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Domain.Entities;
using Dashboard_v2.Domain.Enums;

namespace Dashboard_v2.Application.Documents.Reports;

/// <summary>
/// Reporte del anexo de publicaciones científicas.
/// URL generada: GET /api/Documents/anexo-publicaciones
/// Plantilla:    Infrastructure/Templates/AnexoPublicaciones.xlsx
///
/// La primera hoja es informativa y no se rellena automáticamente.
/// El resto de las hojas sí reciben datos dinámicos clasificados por:
/// G1, G2, G3, G4, libros, monografías, capítulos de libro y artículos de divulgación.
/// </summary>
public sealed class AnexoPublicacionesReport : IDocumentReport
{
    private readonly IApplicationDbContext _context;

    /// <summary>
    /// Inicializa el reporte con acceso al contexto de aplicación.
    /// </summary>
    /// <param name="context">Contexto de base de datos usado para consultar publicaciones.</param>
    public AnexoPublicacionesReport(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Identificador público del reporte consumido por el endpoint genérico.
    /// </summary>
    public string ReportName => "anexo-publicaciones";

    /// <summary>
    /// Nombre base de la plantilla embebida sin extensión.
    /// </summary>
    public string TemplateName => "AnexoPublicaciones";

    /// <summary>
    /// Reúne y clasifica todas las publicaciones necesarias para el anexo.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Variables cuyo nombre coincide con los rangos definidos en la plantilla.</returns>
    public async Task<IReadOnlyDictionary<string, object>> GatherVariablesAsync(CancellationToken ct)
    {
        var publications = await _context.Publications
            .AsNoTracking()
            .Include(p => p.AuthorPublications)
                .ThenInclude(ap => ap.Author)
            .Include(p => p.JournalPublication)
                .ThenInclude(jp => jp!.JournalGroup1Publication)
            .Include(p => p.IndexedPublication)
            .OrderBy(p => p.Title)
            .ToListAsync(ct);

        var journalPublications = publications
            .Where(p => p.PublicationType == PublicationType.Diario && p.JournalPublication is not null)
            .OrderBy(p => p.Title)
            .ToList();

        var indexedPublications = publications
            .Where(p => p.PublicationType != PublicationType.Diario && p.IndexedPublication is not null)
            .OrderBy(p => p.Title)
            .ToList();

        return new Dictionary<string, object>
        {
            ["G1"] = journalPublications
                .Where(p => p.JournalPublication!.Group == 1)
                .Select((p, index) => new PublicacionG1RowDto
                {
                    No = index + 1,
                    Titulo = p.Title,
                    DatosPublicacion = BuildPublicationDetails(p),
                    RelacionAutoria = BuildAuthorsSummary(p),
                    BaseDeDatos = p.JournalPublication!.DataBase,
                    Cuartil = p.JournalPublication.JournalGroup1Publication?.Cuartil ?? string.Empty,
                })
                .ToList(),
            ["G2"] = journalPublications
                .Where(p => p.JournalPublication!.Group == 2)
                .Select((p, index) => BuildJournalRow(p, index))
                .ToList(),
            ["G3"] = journalPublications
                .Where(p => p.JournalPublication!.Group == 3)
                .Select((p, index) => BuildJournalRow(p, index))
                .ToList(),
            ["G4"] = journalPublications
                .Where(p => p.JournalPublication!.Group == 4)
                .Select((p, index) => BuildJournalRow(p, index))
                .ToList(),
            ["Libros"] = indexedPublications
                .Where(p => p.PublicationType == PublicationType.Libro)
                .Select((p, index) => BuildIndexedRow(p, index))
                .ToList(),
            ["Monografias"] = indexedPublications
                .Where(p => p.PublicationType == PublicationType.Monografía)
                .Select((p, index) => BuildIndexedRow(p, index))
                .ToList(),
            ["Capitulos"] = indexedPublications
                .Where(p => p.PublicationType == PublicationType.Capítulo)
                .Select((p, index) => BuildIndexedRow(p, index))
                .ToList(),
            ["ArticulosDivulgacion"] = indexedPublications
                .Where(p => p.PublicationType == PublicationType.Artículo_de_Divulgación)
                .Select((p, index) => new PublicacionDivulgacionRowDto
                {
                    No = index + 1,
                    Titulo = p.Title,
                    DatosPublicacion = BuildPublicationDetails(p),
                    RelacionAutoria = BuildAuthorsSummary(p),
                })
                .ToList(),
        };
    }

    /// <summary>
    /// Construye una fila para los grupos G2-G4, donde no existe columna de cuartil.
    /// </summary>
    /// <param name="publication">Publicación de revista a proyectar.</param>
    /// <param name="index">Posición cero basada dentro del grupo.</param>
    /// <returns>Fila lista para ClosedXML.Report.</returns>
    private static PublicacionJournalRowDto BuildJournalRow(Publication publication, int index)
    {
        return new PublicacionJournalRowDto
        {
            No = index + 1,
            Titulo = publication.Title,
            DatosPublicacion = BuildPublicationDetails(publication),
            RelacionAutoria = BuildAuthorsSummary(publication),
            BaseDeDatos = publication.JournalPublication!.DataBase,
        };
    }

    /// <summary>
    /// Construye una fila para libros, monografías y capítulos de libro.
    /// </summary>
    /// <param name="publication">Publicación indexada a proyectar.</param>
    /// <param name="index">Posición cero basada dentro de la categoría.</param>
    /// <returns>Fila lista para ClosedXML.Report.</returns>
    private static PublicacionIndexadaRowDto BuildIndexedRow(Publication publication, int index)
    {
        return new PublicacionIndexadaRowDto
        {
            No = index + 1,
            Indexacion = string.Empty,
            Titulo = publication.Title,
            DatosEditorial = publication.PublicationData,
            RelacionAutoria = BuildAuthorsSummary(publication),
        };
    }

    /// <summary>
    /// Construye el texto de autores mostrado en el anexo.
    /// Como el modelo actual no persiste el orden de autoría, se usa orden alfabético
    /// para garantizar un resultado estable y reproducible.
    /// </summary>
    /// <param name="publication">Publicación cuyo listado de autores se formateará.</param>
    /// <returns>Cadena con los autores separados por coma.</returns>
    private static string BuildAuthorsSummary(Publication publication)
    {
        return string.Join(", ", publication.AuthorPublications
            .Where(ap => ap.Author is not null)
            .Select(ap => ap.Author.Name)
            .OrderBy(name => name));
    }

    /// <summary>
    /// Construye el bloque descriptivo de la publicación combinando el texto base
    /// almacenado en el sistema con el DOI o URL directa cuando exista.
    /// </summary>
    /// <param name="publication">Publicación a describir.</param>
    /// <returns>Texto consolidado de datos bibliográficos.</returns>
    private static string BuildPublicationDetails(Publication publication)
    {
        if (string.IsNullOrWhiteSpace(publication.UrlDoi))
        {
            return publication.PublicationData;
        }

        return $"{publication.PublicationData} DOI/URL: {publication.UrlDoi}";
    }
}

/// <summary>
/// Fila para publicaciones seriadas G2-G4.
/// </summary>
public record PublicacionJournalRowDto
{
    /// <summary>
    /// Numeración consecutiva dentro del grupo.
    /// </summary>
    public int No { get; init; }

    /// <summary>
    /// Título de la publicación.
    /// </summary>
    public string Titulo { get; init; } = string.Empty;

    /// <summary>
    /// Datos bibliográficos consolidados.
    /// </summary>
    public string DatosPublicacion { get; init; } = string.Empty;

    /// <summary>
    /// Relación de autoría mostrada en el anexo.
    /// </summary>
    public string RelacionAutoria { get; init; } = string.Empty;

    /// <summary>
    /// Base de datos o índice de la revista.
    /// </summary>
    public string BaseDeDatos { get; init; } = string.Empty;
}

/// <summary>
/// Fila para publicaciones seriadas G1, que además incorpora cuartil.
/// </summary>
public sealed record PublicacionG1RowDto : PublicacionJournalRowDto
{
    /// <summary>
    /// Cuartil asociado a la publicación del grupo 1.
    /// </summary>
    public string Cuartil { get; init; } = string.Empty;
}

/// <summary>
/// Fila para libros, monografías y capítulos de libro.
/// </summary>
public sealed record PublicacionIndexadaRowDto
{
    /// <summary>
    /// Numeración consecutiva dentro de la categoría.
    /// </summary>
    public int No { get; init; }

    /// <summary>
    /// Texto de indexación o referencia editorial.
    /// </summary>
    public string Indexacion { get; init; } = string.Empty;

    /// <summary>
    /// Título de la publicación.
    /// </summary>
    public string Titulo { get; init; } = string.Empty;

    /// <summary>
    /// Datos editoriales consolidados.
    /// </summary>
    public string DatosEditorial { get; init; } = string.Empty;

    /// <summary>
    /// Relación de autoría mostrada en el anexo.
    /// </summary>
    public string RelacionAutoria { get; init; } = string.Empty;
}

/// <summary>
/// Fila para artículos de divulgación.
/// </summary>
public sealed record PublicacionDivulgacionRowDto
{
    /// <summary>
    /// Numeración consecutiva dentro de la categoría.
    /// </summary>
    public int No { get; init; }

    /// <summary>
    /// Título de la publicación.
    /// </summary>
    public string Titulo { get; init; } = string.Empty;

    /// <summary>
    /// Datos bibliográficos consolidados.
    /// </summary>
    public string DatosPublicacion { get; init; } = string.Empty;

    /// <summary>
    /// Relación de autoría mostrada en el anexo.
    /// </summary>
    public string RelacionAutoria { get; init; } = string.Empty;
}
