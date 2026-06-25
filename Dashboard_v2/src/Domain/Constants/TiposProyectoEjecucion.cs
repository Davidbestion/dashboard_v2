using Dashboard_v2.Domain.Entities;

namespace Dashboard_v2.Domain.Constants;

/// <summary>
/// Registry of valid project type identifiers for executing projects.
/// Discovered via reflection over concrete ProyectoEnEjecucion subclasses.
/// </summary>
public static class TiposProyectoEjecucion
{
    /// <summary>
    /// Set of valid project type identifiers (e.g. 'PE', 'PAP', 'PDL').
    /// Populated at startup by scanning ProyectoEnEjecucion subclasses.
    /// </summary>
    public static readonly IReadOnlySet<string> Validos =
        // Reflection discovers all concrete ProyectoEnEjecucion subclasses and instantiates them
        // to read their TipoIdentificador. This avoids maintaining a manual list that could go stale.
        typeof(ProyectoEnEjecucion).Assembly
            .GetTypes()
            .Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(ProyectoEnEjecucion)))
            .Select(t => (ProyectoEnEjecucion)Activator.CreateInstance(t)!)
            .Select(i => i.TipoIdentificador)
            .ToHashSet(StringComparer.Ordinal);
}
