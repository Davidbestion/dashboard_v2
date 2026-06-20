using Dashboard_v2.Application.Common.Models;

namespace Dashboard_v2.Application.Patentes;

/// <summary>
/// Servicio de gestión de patentes: listados con filtro por área, vínculos con
/// proyectos y CRUD con verificación de autoría.
/// </summary>
public interface IPatenteService
{
    /// <summary>Listado general; aplica filtro por área cuando el usuario actual es Vicedecano.</summary>
    Task<List<PatenteDto>> GetAllAsync(CancellationToken ct = default);

    Task<List<PatenteDto>> GetMisAsync(CancellationToken ct = default);

    Task<(bool Found, List<ProyectoPatenteDto> Proyectos)> GetProyectosDeAsync(string patenteId, CancellationToken ct = default);

    Task<Result> LinkProyectoAsync(string patenteId, string proyectoId, CancellationToken ct = default);

    Task<Result> UnlinkProyectoAsync(string patenteId, string proyectoId, CancellationToken ct = default);

    Task<(Result Result, string? Id)> CreateAsync(CreatePatenteBody body, CancellationToken ct = default);

    Task<Result> UpdateAsync(string id, UpdatePatenteBody body, CancellationToken ct = default);

    Task<Result> DeleteAsync(string id, CancellationToken ct = default);
}
