using Dashboard_v2.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Dashboard_v2.Application.Common;

/// <summary>
/// Generic service for adding co-authors/creators to research outputs (publications, patents, registries).
/// Uses a factory to build the join entity.
/// </summary>
public sealed class ProductionCreatorService : IProductionCreatorService
{
    private readonly IApplicationDbContext _db;
    private readonly IAuthorResolutionService _authorResolution;

    public ProductionCreatorService(IApplicationDbContext db, IAuthorResolutionService authorResolution)
    {
        _db = db;
        _authorResolution = authorResolution;
    }

    public async Task AddAdditionalCreatorsAsync<TJoin>(
        ICollection<TJoin> creadores,
        string currentAuthorId,
        Func<string, TJoin> joinFactory,
        Func<TJoin, string> getAuthorId,
        IEnumerable<string>? additionalAuthorIds,
        IEnumerable<string>? additionalAuthorNames,
        IEnumerable<string>? additionalUserIds,
        CancellationToken cancellationToken = default)
        where TJoin : class
    {
        foreach (var authorId in (additionalAuthorIds ?? []).Where(id => !string.IsNullOrWhiteSpace(id)))
        {
            if (authorId == currentAuthorId) continue;
            if (creadores.Any(c => getAuthorId(c) == authorId)) continue;
            if (!await _db.Authors.AnyAsync(a => a.Id == authorId, cancellationToken)) continue;
            creadores.Add(joinFactory(authorId));
        }

        foreach (var authorName in (additionalAuthorNames ?? []).Where(name => !string.IsNullOrWhiteSpace(name)))
        {
            var resolved = await _authorResolution.ResolveByNameAsync(authorName, cancellationToken);
            if (resolved.Id == currentAuthorId) continue;
            if (creadores.Any(c => getAuthorId(c) == resolved.Id)) continue;
            creadores.Add(joinFactory(resolved.Id));
        }

        foreach (var userId in (additionalUserIds ?? []).Where(id => !string.IsNullOrWhiteSpace(id)))
        {
            var resolved = await _authorResolution.GetOrCreateForUserAsync(userId, cancellationToken);
            if (resolved == null || resolved.Id == currentAuthorId) continue;
            if (creadores.Any(c => getAuthorId(c) == resolved.Id)) continue;
            creadores.Add(joinFactory(resolved.Id));
        }
    }
}
