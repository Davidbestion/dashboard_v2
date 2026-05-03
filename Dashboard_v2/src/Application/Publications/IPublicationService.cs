using Dashboard_v2.Application.Common.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Dashboard_v2.Application.Publications;

public interface IPublicationService
{
    Task<(Result Result, string? PublicationId)> CreateAsync(CreatePublicationRequest request, CancellationToken ct = default);
    Task<Result> UpdateAsync(UpdatePublicationRequest request, CancellationToken ct = default);
    Task<Result> DeleteAsync(string id, CancellationToken ct = default);
    Task<PublicationDto?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<PublicationDto?> GetPublicByIdAsync(string id, CancellationToken ct = default);
    Task<List<PublicationDto>> GetMyPublicationsAsync(CancellationToken ct = default);
    Task<List<PublicationDto>> GetAllPublicationsAsync(CancellationToken ct = default);
    Task<List<PublicationTypeDto>> GetPublicationTypesAsync();
    Task<List<PublicationCrossRefDto>> SearchCrossRefCandidatesAsync(string? doi, string? title, CancellationToken ct = default);
    Task<List<PublicationCrossRefDto>> SearchOpenAireCandidatesAsync(string? doi, string? title, CancellationToken ct = default);
    Task<List<PublicationDuplicateDto>> FindDuplicatesAsync(string? title, string? doi, string? url, string? excludePublicationId = null, CancellationToken ct = default);
    Task<Result> AddCurrentUserAsCoauthorAsync(string publicationId, CancellationToken ct = default);
}
