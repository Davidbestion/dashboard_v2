using Dashboard_v2.Application.Common.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Dashboard_v2.Application.Awards;

public interface IAwardService
{
    Task<List<AwardDto>> GetMyAwardsAsync(CancellationToken ct = default);
    Task<(Result Result, int? AwardedId)> CreateAsync(CreateAwardRequest request, CancellationToken ct = default);
    Task<Result> UpdateAsync(int id, UpdateAwardRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CancellationToken ct = default);
}
