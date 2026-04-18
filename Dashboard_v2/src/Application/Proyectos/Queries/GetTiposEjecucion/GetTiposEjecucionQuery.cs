using Dashboard_v2.Domain.Constants;

namespace Dashboard_v2.Application.Proyectos.Queries.GetTiposEjecucion;

/// <summary>
/// Devuelve los identificadores de tipo disponibles para <see cref="Domain.Entities.ProyectoEnRevision.Tipo"/>
/// (p.ej. "PE", "PAP", "PDL", "PNE", "PRCI", "PNAP").
/// El conjunto se descubre dinámicamente a partir de las subclases concretas de
/// <see cref="Domain.Entities.ProyectoEnEjecucion"/>; no toca la base de datos.
/// </summary>
public record GetTiposEjecucionQuery : IRequest<IReadOnlyList<string>>;

/// <summary>Handler de <see cref="GetTiposEjecucionQuery"/>.</summary>
public class GetTiposEjecucionQueryHandler
    : IRequestHandler<GetTiposEjecucionQuery, IReadOnlyList<string>>
{
    public Task<IReadOnlyList<string>> Handle(GetTiposEjecucionQuery request, CancellationToken ct)
    {
        IReadOnlyList<string> result = TiposProyectoEjecucion.Validos
            .Order()
            .ToList();
        return Task.FromResult(result);
    }
}
