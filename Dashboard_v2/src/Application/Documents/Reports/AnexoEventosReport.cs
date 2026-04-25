using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dashboard_v2.Application.Documents.Reports;

/// <summary>
/// Reporte del anexo de eventos y actividades cientificas.
/// URL generada: GET /api/Documents/anexo-eventos
/// Plantilla:    Infrastructure/Templates/AnexoEventosCientificos.xlsx
///
/// Bloques rellenados automaticamente:
/// - Eventos internacionales.
/// - Eventos nacionales.
/// - Conteo base de ponencias por categorias explicitamente modeladas.
/// - Datos detallados de todas las ponencias.
///
/// Bloques que quedan vacios o parcialmente vacios:
/// - Eventos coauspiciados por el area.
/// - Actividades cientificas en la UH.
/// - Las columnas sobre primer autor del area y autores de otras areas.
/// - Toda la seccion de conferencias magistrales.
///
/// Estas restricciones existen porque el dominio actual no guarda orden de autoria,
/// no asocia el anexo a un area concreta, no distingue eventos coauspiciados
/// ni actividades cientificas internas, y tampoco diferencia conferencias
/// magistrales de otras presentaciones.
/// </summary>
public sealed class AnexoEventosReport : IDocumentReport
{
    // TODO(david): Este reporte sigue alimentando una plantilla de ClosedXML.Report que
    // pierde formato cuando expande varias tablas dinámicas en la misma hoja.
    // Opciones para arreglarlo:
    // 1. Dejar de devolver variables para una hoja dinámica y construir el Excel manualmente.
    // 2. Conservar la plantilla solo como guía visual y rellenar celdas fijas.
    // 3. Repartir las secciones en hojas separadas si eso es aceptable.
    // 4. Rediseñar la hoja para eliminar merges y bloques sensibles al corrimiento de filas.
    private const int InternacionalEventTypeId = 0;
    private const int NacionalEventTypeId = 1;

    private readonly IApplicationDbContext _context;

    /// <summary>
    /// Inicializa el reporte con acceso al contexto de aplicacion.
    /// </summary>
    /// <param name="context">Contexto usado para consultar eventos y ponencias.</param>
    public AnexoEventosReport(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Identificador publico del reporte consumido por el endpoint generico.
    /// </summary>
    public string ReportName => "anexo-eventos";

    /// <summary>
    /// Nombre base de la plantilla embebida sin extension.
    /// </summary>
    public string TemplateName => "AnexoEventosCientificos";

    /// <summary>
    /// Reune y clasifica los eventos y presentaciones necesarias para el anexo.
    /// Las claves del diccionario coinciden con los rangos nombrados y variables
    /// escalares declaradas en la plantilla.
    /// </summary>
    /// <param name="ct">Token de cancelacion.</param>
    /// <returns>Variables para ClosedXML.Report.</returns>
    public async Task<IReadOnlyDictionary<string, object>> GatherVariablesAsync(CancellationToken ct)
    {
        var events = await _context.Events
            .AsNoTracking()
            .Include(e => e.Country)
            .Include(e => e.Presentations)
                .ThenInclude(p => p.AuthorPresentations)
                    .ThenInclude(ap => ap.Author)
            .OrderBy(e => e.Name)
            .ToListAsync(ct);

        var presentations = events
            .SelectMany(e => e.Presentations.Select(p => new { Event = e, Presentation = p }))
            .OrderBy(item => item.Event.Name)
            .ThenBy(item => item.Presentation.Name)
            .ToList();

        var ponenciasInternacionalesExtranjero = presentations.Count(item =>
            item.Event.EventTypeId == InternacionalEventTypeId && !IsHeldInCuba(item.Event));

        var ponenciasInternacionalesCuba = presentations.Count(item =>
            item.Event.EventTypeId == InternacionalEventTypeId && IsHeldInCuba(item.Event));

        var ponenciasNacionalesCuba = presentations.Count(item =>
            item.Event.EventTypeId == NacionalEventTypeId);

        const int ponenciasActividadesUh = 0;

        return new Dictionary<string, object>
        {
            ["EventosInternacionales"] = events
                .Where(e => e.EventTypeId == InternacionalEventTypeId)
                .Select(e => new EventoInternacionalRowDto
                {
                    NombreEventoInternacional = e.Name,
                    PaisSiFueEnElExtranjero = SelectColumnValue(e, heldInCuba: false, GetCountryName(e)),
                    EnCuba = SelectColumnValue(e, heldInCuba: true, GetCountryName(e)),
                })
                .ToList(),
            ["EventosNacionales"] = events
                .Where(e => e.EventTypeId == NacionalEventTypeId)
                .Select(e => new EventoNacionalRowDto
                {
                    NombreEventoNacional = e.Name,
                    InstitucionQueLoOrganizo = BuildInstitutionsSummary(e),
                })
                .ToList(),
            // Estos apartados existen en el anexo oficial, pero el dominio actual
            // no modela si un evento fue coauspiciado por el area ni si una
            // actividad cientifica fue organizada internamente en la UH.
            ["EventosCoauspiciados"] = Array.Empty<EventoCoauspiciadoRowDto>(),
            ["ActividadesCientificasUH"] = Array.Empty<ActividadCientificaUhRowDto>(),
            ["DatosPonencias"] = presentations
                .Select(item => new DatosPonenciaRowDto
                {
                    NombrePonencia = item.Presentation.Name,
                    NombreAutores = BuildAuthorsSummary(item.Presentation),
                    NombreEventoOActividadCientifica = item.Event.Name,
                    PaisDeCelebracion = item.Event.Country.Name,
                })
                .ToList(),
            ["PonenciasInternacionalesExtranjero"] = ponenciasInternacionalesExtranjero,
            ["PonenciasInternacionalesCuba"] = ponenciasInternacionalesCuba,
            ["PonenciasNacionalesCuba"] = ponenciasNacionalesCuba,
            ["PonenciasActividadesUH"] = ponenciasActividadesUh,
            ["PonenciasTotal"] = ponenciasInternacionalesExtranjero
                + ponenciasInternacionalesCuba
                + ponenciasNacionalesCuba
                + ponenciasActividadesUh,
        };
    }

    /// <summary>
    /// Determina si un evento se celebro en Cuba usando el pais asociado.
    /// </summary>
    /// <param name="eventEntity">Evento a evaluar.</param>
    /// <returns><see langword="true"/> si el pais es Cuba; en otro caso, <see langword="false"/>.</returns>
    private static bool IsHeldInCuba(Event eventEntity)
    {
        return string.Equals(eventEntity.Country.Name?.Trim(), "Cuba", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Devuelve el texto a colocar en una columna condicional del anexo.
    /// Si el evento cumple la condicion de destino, se escribe el valor; si no,
    /// la celda queda vacia.
    /// </summary>
    private static string SelectColumnValue(Event eventEntity, bool heldInCuba, string value)
    {
        return IsHeldInCuba(eventEntity) == heldInCuba ? value : string.Empty;
    }

    /// <summary>
    /// Obtiene el pais del evento con un valor estable para exportacion.
    /// </summary>
    private static string GetCountryName(Event eventEntity)
    {
        return eventEntity.Country.Name?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Construye el texto de instituciones organizadoras externas del evento.
    /// </summary>
    /// <param name="eventEntity">Evento cuyas instituciones se formatearan.</param>
    /// <returns>Cadena unica y estable con las instituciones separadas por coma.</returns>
    private static string BuildInstitutionsSummary(Event eventEntity)
    {
        return string.Join(", ", eventEntity.Institutions
            .Where(i => !string.IsNullOrWhiteSpace(i))
            .Select(i => i.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(i => i));
    }

    /// <summary>
    /// Construye la relacion de autoria de una ponencia.
    /// Como el modelo no conserva orden de autores, se usa orden alfabetico para
    /// generar una salida estable y reproducible.
    /// </summary>
    /// <param name="presentation">Ponencia a proyectar.</param>
    /// <returns>Nombres de autores separados por coma.</returns>
    private static string BuildAuthorsSummary(Presentation presentation)
    {
        return string.Join(", ", presentation.AuthorPresentations
            .Where(ap => ap.Author is not null)
            .Select(ap => ap.Author.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .OrderBy(name => name));
    }
}

/// <summary>
/// Fila para la tabla de eventos internacionales.
/// </summary>
public sealed record EventoInternacionalRowDto
{
    /// <summary>
    /// Nombre del evento internacional.
    /// </summary>
    public string NombreEventoInternacional { get; init; } = string.Empty;

    /// <summary>
    /// Pais del evento cuando se celebro fuera de Cuba.
    /// </summary>
    public string PaisSiFueEnElExtranjero { get; init; } = string.Empty;

    /// <summary>
    /// Pais del evento cuando se celebro en Cuba.
    /// </summary>
    public string EnCuba { get; init; } = string.Empty;
}

/// <summary>
/// Fila para la tabla de eventos nacionales.
/// </summary>
public sealed record EventoNacionalRowDto
{
    /// <summary>
    /// Nombre del evento nacional.
    /// </summary>
    public string NombreEventoNacional { get; init; } = string.Empty;

    /// <summary>
    /// Instituciones organizadoras reportadas en el sistema.
    /// </summary>
    public string InstitucionQueLoOrganizo { get; init; } = string.Empty;
}

/// <summary>
/// Fila para la tabla de eventos coauspiciados por el area.
/// </summary>
public sealed record EventoCoauspiciadoRowDto
{
    /// <summary>
    /// Nombre del evento coauspiciado.
    /// </summary>
    public string EventoCoauspiciado { get; init; } = string.Empty;

    /// <summary>
    /// Institucion externa responsable del evento.
    /// </summary>
    public string InstitucionExternaResponsable { get; init; } = string.Empty;

    /// <summary>
    /// Marca de alcance internacional inferida a partir del pais.
    /// </summary>
    public string Internacional { get; init; } = string.Empty;

    /// <summary>
    /// Marca de alcance nacional inferida a partir del pais.
    /// </summary>
    public string Nacional { get; init; } = string.Empty;
}

/// <summary>
/// Fila para la tabla de actividades cientificas celebradas en la UH.
/// </summary>
public sealed record ActividadCientificaUhRowDto
{
    /// <summary>
    /// Nombre de la actividad cientifica.
    /// </summary>
    public string ActividadCientifica { get; init; } = string.Empty;
}

/// <summary>
/// Fila detallada de una ponencia presentada.
/// </summary>
public sealed record DatosPonenciaRowDto
{
    /// <summary>
    /// Nombre de la ponencia.
    /// </summary>
    public string NombrePonencia { get; init; } = string.Empty;

    /// <summary>
    /// Relacion de autores asociada a la ponencia.
    /// </summary>
    public string NombreAutores { get; init; } = string.Empty;

    /// <summary>
    /// Nombre del evento o actividad donde se presento la ponencia.
    /// </summary>
    public string NombreEventoOActividadCientifica { get; init; } = string.Empty;

    /// <summary>
    /// Pais de celebracion del evento.
    /// </summary>
    public string PaisDeCelebracion { get; init; } = string.Empty;
}
