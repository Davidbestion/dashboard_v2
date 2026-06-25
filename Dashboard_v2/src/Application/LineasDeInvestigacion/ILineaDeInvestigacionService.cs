using Dashboard_v2.Application.Common.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Dashboard_v2.Application.LineasDeInvestigacion;

/// <summary>
/// Application service for managing research lines: listing and CRUD operations.
/// </summary>
public interface ILineaDeInvestigacionService
{
    Task<List<LineaDeInvestigacionDto>> GetAllAsync(CancellationToken ct = default);
    Task<(Result Result, string? Id)> CreateAsync(CreateLineaDeInvestigacionRequest command, CancellationToken ct = default);
    Task<Result> UpdateAsync(string id, UpdateLineaDeInvestigacionRequest command, CancellationToken ct = default);
    Task<Result> DeleteAsync(string id, CancellationToken ct = default);
}
