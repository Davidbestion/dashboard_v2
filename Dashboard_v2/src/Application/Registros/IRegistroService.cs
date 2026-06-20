using Dashboard_v2.Application.Common.Models;

namespace Dashboard_v2.Application.Registros;

/// <summary>Servicio de gestión de registros: listados con filtro por área y CRUD con verificación de autoría.</summary>
public interface IRegistroService
{
    /// <summary>Listado general; aplica filtro por área cuando el usuario actual es Vicedecano.</summary>
    Task<List<RegistroDto>> GetAllAsync(CancellationToken ct = default);

    Task<List<RegistroDto>> GetMisAsync(CancellationToken ct = default);

    Task<(Result Result, string? Id)> CreateAsync(CreateRegistroBody body, CancellationToken ct = default);

    Task<Result> UpdateAsync(string id, UpdateRegistroBody body, CancellationToken ct = default);

    Task<Result> DeleteAsync(string id, CancellationToken ct = default);
}
