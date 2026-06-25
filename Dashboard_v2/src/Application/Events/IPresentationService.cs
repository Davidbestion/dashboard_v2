using Dashboard_v2.Application.Common.Models;

namespace Dashboard_v2.Application.Events;

/// <summary>
/// Application service for managing academic presentations (ponencias) within events.
/// </summary>
public interface IPresentationService
{
    Task<List<PresentationDto>> GetMyPresentationsAsync(CancellationToken ct = default);
    Task<List<PresentationDto>> GetAllPresentationsAsync(CancellationToken ct = default);
    Task<List<PresentationDto>> GetAreaPresentationsAsync(CancellationToken ct = default);
    Task<(Result Result, int? PresentationId)> CreatePresentationAsync(CreatePresentationRequest request, CancellationToken ct = default);
    Task<Result> UpdatePresentationAsync(int id, UpdatePresentationRequest request, CancellationToken ct = default);
    Task<Result> DeletePresentationAsync(int id, CancellationToken ct = default);
}
