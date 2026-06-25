using Dashboard_v2.Application.Common.Models;

namespace Dashboard_v2.Application.Proyectos;

/// <summary>
/// Write-side service for research projects: create, update, delete, and relationship management (participants, publications, patents).
/// </summary>
public interface IProyectoCommandService
{
    Task<Result> DeleteAsync(string id, CancellationToken ct = default);
    Task<Result> SetParticipantesAsync(string proyectoId, IList<string> participantesIds, CancellationToken ct = default);
    Task<Result> LinkPublicacionAsync(string proyectoId, string publicationId, CancellationToken ct = default);
    Task<Result> UnlinkPublicacionAsync(string proyectoId, string publicationId, CancellationToken ct = default);
    Task<Result> LinkPatenteAsync(string proyectoId, string patenteId, CancellationToken ct = default);
    Task<Result> UnlinkPatenteAsync(string proyectoId, string patenteId, CancellationToken ct = default);
    Task<(Result Result, string? Id)> CreateEnRevisionAsync(ProyectoEnRevisionUpsertRequest request, CancellationToken ct = default);
    Task<Result> UpdateEnRevisionAsync(string id, ProyectoEnRevisionUpsertRequest request, CancellationToken ct = default);
    Task<(Result Result, string? Id)> CreateEmpresarialAsync(ProyectoEmpresarialUpsertRequest request, CancellationToken ct = default);
    Task<Result> UpdateEmpresarialAsync(string id, ProyectoEmpresarialUpsertRequest request, CancellationToken ct = default);
    Task<(Result Result, string? Id)> CreateApoyoProgramaAsync(ProyectoApoyoProgramaUpsertRequest request, CancellationToken ct = default);
    Task<Result> UpdateApoyoProgramaAsync(string id, ProyectoApoyoProgramaUpsertRequest request, CancellationToken ct = default);
    Task<(Result Result, string? Id)> CreateDesarrolloLocalAsync(ProyectoDesarrolloLocalUpsertRequest request, CancellationToken ct = default);
    Task<Result> UpdateDesarrolloLocalAsync(string id, ProyectoDesarrolloLocalUpsertRequest request, CancellationToken ct = default);
    Task<(Result Result, string? Id)> CreateNoEmpresarialAsync(ProyectoNoEmpresarialUpsertRequest request, CancellationToken ct = default);
    Task<Result> UpdateNoEmpresarialAsync(string id, ProyectoNoEmpresarialUpsertRequest request, CancellationToken ct = default);
    Task<(Result Result, string? Id)> CreateColabInternacionalAsync(ProyectoColabInternacionalUpsertRequest request, CancellationToken ct = default);
    Task<Result> UpdateColabInternacionalAsync(string id, ProyectoColabInternacionalUpsertRequest request, CancellationToken ct = default);
    Task<(Result Result, string? Id)> CreatePNAPAsync(ProyectoPNAPUpsertRequest request, CancellationToken ct = default);
    Task<Result> UpdatePNAPAsync(string id, ProyectoPNAPUpsertRequest request, CancellationToken ct = default);
}
