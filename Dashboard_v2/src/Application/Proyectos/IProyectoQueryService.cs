using Dashboard_v2.Application.Common.Models;

namespace Dashboard_v2.Application.Proyectos;

public interface IProyectoQueryService
{
    Task<List<ProyectoResumenDto>> GetAllAsync(CancellationToken ct = default);
    Task<List<ProyectoResumenDto>> GetAreaProyectosAsync(CancellationToken ct = default);
    Task<List<ProyectoResumenDto>> GetMisProyectosParticipacionAsync(CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetTiposEjecucionAsync(CancellationToken ct = default);
    Task<List<ProyectoCatalogoDto>> GetCatalogoAsync(CancellationToken ct = default);
    Task<List<ProyectoPublicacionDto>> GetPublicacionesDelProyectoAsync(string proyectoId, CancellationToken ct = default);
    Task<List<ProyectoPublicacionDto>> GetPublicacionesDisponiblesAsync(CancellationToken ct = default);
    Task<List<ProyectoPatenteResumenDto>> GetPatentesDelProyectoAsync(string proyectoId, CancellationToken ct = default);
    Task<ProyectoEnRevisionDto?> GetEnRevisionByIdAsync(string id, CancellationToken ct = default);
    Task<ProyectoEmpresarialDto?> GetEmpresarialByIdAsync(string id, CancellationToken ct = default);
    Task<ProyectoApoyoProgramaDto?> GetApoyoProgramaByIdAsync(string id, CancellationToken ct = default);
    Task<ProyectoDesarrolloLocalDto?> GetDesarrolloLocalByIdAsync(string id, CancellationToken ct = default);
    Task<ProyectoNoEmpresarialDto?> GetNoEmpresarialByIdAsync(string id, CancellationToken ct = default);
    Task<ProyectoColabInternacionalDto?> GetColabInternacionalByIdAsync(string id, CancellationToken ct = default);
    Task<ProyectoPNAPDto?> GetPNAPByIdAsync(string id, CancellationToken ct = default);
}
