using Dashboard_v2.Application.Common.Models;

namespace Dashboard_v2.Application.Normas;

/// <summary>Servicio de gestión de normas técnicas: listados con filtro por área y CRUD con verificación de autoría.</summary>
public interface INormaService
{
    /// <summary>Listado general; aplica filtro por área cuando el usuario actual es Vicedecano.</summary>
    Task<List<NormaDto>> GetAllAsync(CancellationToken ct = default);

    Task<List<NormaDto>> GetMisAsync(CancellationToken ct = default);

    Task<(Result Result, string? Id)> CreateAsync(CreateNormaBody body, CancellationToken ct = default);

    Task<Result> UpdateAsync(string id, UpdateNormaBody body, CancellationToken ct = default);

    Task<Result> DeleteAsync(string id, CancellationToken ct = default);
}
