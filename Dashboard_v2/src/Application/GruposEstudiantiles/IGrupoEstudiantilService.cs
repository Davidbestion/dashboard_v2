using Dashboard_v2.Application.Common.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Dashboard_v2.Application.GruposEstudiantiles;

/// <summary>
/// Application service for managing student research groups: CRUD operations.
/// </summary>
public interface IGrupoEstudiantilService
{
    Task<List<GrupoEstudiantilDto>> GetAllAsync(CancellationToken ct = default);
    Task<List<GrupoEstudiantilDto>> GetAreaAsync(CancellationToken ct = default);
    Task<(Result Result, string? Id)> CreateAsync(CreateGrupoEstudiantilRequest command, CancellationToken ct = default);
    Task<Result> UpdateAsync(string id, UpdateGrupoEstudiantilRequest command, CancellationToken ct = default);
    Task<Result> DeleteAsync(string id, CancellationToken ct = default);
}
