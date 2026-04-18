using Dashboard_v2.Domain.Entities;

namespace Dashboard_v2.Domain.Constants;

/// <summary>
/// Conjunto de identificadores válidos para <see cref="ProyectoEnRevision.Tipo"/>.
/// Se descubre automáticamente a partir de las subclases concretas de
/// <see cref="ProyectoEnEjecucion"/> presentes en el assembly de dominio;
/// no requiere mantenimiento manual al agregar nuevos tipos.
/// </summary>
public static class TiposProyectoEjecucion
{
    /// <summary>
    /// Set inmutable con los <see cref="ProyectoEnEjecucion.TipoIdentificador"/> de todas
    /// las subclases concretas (p.ej. "PE", "PAP", "PDL", "PNE", "PRCI", "PNAP").
    /// </summary>
    public static readonly IReadOnlySet<string> Validos =
        typeof(ProyectoEnEjecucion).Assembly
            .GetTypes()
            .Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(ProyectoEnEjecucion)))
            .Select(t => (ProyectoEnEjecucion)Activator.CreateInstance(t)!)
            .Select(i => i.TipoIdentificador)
            .ToHashSet(StringComparer.Ordinal);
}
