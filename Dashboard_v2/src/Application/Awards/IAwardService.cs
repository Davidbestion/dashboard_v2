using Dashboard_v2.Application.Common.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Dashboard_v2.Application.Awards;

/// <summary>
/// Application service for managing awards: catalog browsing, granting, updating, and deletion.
/// </summary>
public interface IAwardService
{
    Task<List<AwardWithGrantingsDto>> GetMyAwardsAsync(CancellationToken ct = default);
    Task<List<AwardWithGrantingsDto>> GetAllAwardsAsync(CancellationToken ct = default);
    Task<List<AwardWithGrantingsDto>> GetAreaAwardsAsync(CancellationToken ct = default);
    Task<List<AwardCatalogDto>> GetCatalogAsync(CancellationToken ct = default);
    Task<(Result Result, int? AwardedId)> CreateAsync(CreateAwardRequest request, CancellationToken ct = default);
    Task<Result> UpdateAsync(int id, UpdateAwardRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
}
