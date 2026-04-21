using Dashboard_v2.Application.Common.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Dashboard_v2.Application.Clasificaciones;

public interface IClasificacionService
{
    Task<List<ClasificacionDto>> GetAllAsync(CancellationToken ct = default);
    Task<(Result Result, string? Id)> CreateAsync(CreateClasificacionRequest request, CancellationToken ct = default);
    Task<Result> UpdateAsync(string id, UpdateClasificacionRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(string id, CancellationToken ct = default);
}
