using Dashboard_v2.Application.Common.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Dashboard_v2.Application.GruposDeInvestigacion;

public interface IGrupoDeInvestigacionService
{
    Task<List<GrupoDeInvestigacionDto>> GetAllAsync(CancellationToken ct = default);
    Task<List<GrupoDeInvestigacionDto>> GetMineAsync(CancellationToken ct = default);
    Task<(Result Result, string? Id)> CreateAsync(CreateGrupoDeInvestigacionRequest command, CancellationToken ct = default);
    Task<Result> UpdateAsync(string id, UpdateGrupoDeInvestigacionRequest command, CancellationToken ct = default);
    Task<Result> DeleteAsync(string id, CancellationToken ct = default);
    Task<Result> SetMiembrosAsync(string id, SetGrupoMiembrosRequest command, CancellationToken ct = default);
}
