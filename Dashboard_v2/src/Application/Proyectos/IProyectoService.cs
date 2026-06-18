namespace Dashboard_v2.Application.Proyectos;

/// <summary>
/// Interfaz composita mantenida por compatibilidad con el registro de DI.
/// Los consumidores deben inyectar <see cref="IProyectoQueryService"/> o
/// <see cref="IProyectoCommandService"/> según lo que necesiten.
/// </summary>
public interface IProyectoService : IProyectoQueryService, IProyectoCommandService { }
