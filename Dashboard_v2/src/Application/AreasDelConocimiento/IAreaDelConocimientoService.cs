using Dashboard_v2.Application.Common.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Dashboard_v2.Application.AreasDelConocimiento;

public interface IAreaDelConocimientoService
{
    Task<List<AreaDelConocimientoDto>> GetAllAsync(CancellationToken ct = default);
    Task<(Result Result, string? Id)> CreateAsync(CreateAreaDelConocimientoRequest request, CancellationToken ct = default);
    Task<Result> UpdateAsync(string id, UpdateAreaDelConocimientoRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(string id, CancellationToken ct = default);
}
