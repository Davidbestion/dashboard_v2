using Dashboard_v2.Application.Common.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Dashboard_v2.Application.Areas;

public interface IAreaService
{
    Task<List<AreaDto>> GetAllAsync(CancellationToken ct = default);
    Task<(Result Result, string? Id)> CreateAsync(CreateAreaRequest command, CancellationToken ct = default);
    Task<Result> UpdateAsync(string id, UpdateAreaRequest command, CancellationToken ct = default);
    Task<Result> DeleteAsync(string id, CancellationToken ct = default);
}
