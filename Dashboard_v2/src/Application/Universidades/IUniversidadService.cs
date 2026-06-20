using Dashboard_v2.Application.Common.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Dashboard_v2.Application.Universidades;

public interface IUniversidadService
{
    Task<List<UniversidadDto>> GetAllAsync(CancellationToken ct = default);
    Task<(Result Result, string? Id)> CreateAsync(string nombre, CancellationToken ct = default);
    Task<Result> UpdateAsync(string id, string nombre, CancellationToken ct = default);
    Task<Result> DeleteAsync(string id, CancellationToken ct = default);
}
