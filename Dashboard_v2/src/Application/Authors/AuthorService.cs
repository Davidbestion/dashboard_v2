using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dashboard_v2.Application.Common;
using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace Dashboard_v2.Application.Authors;

/// <summary>
/// Application service for author search, linking, and external-name resolution operations.
/// </summary>
public sealed class AuthorService : IAuthorService
{
    private const int MinSearchTermLength = 2;

    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public AuthorService(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result> LinkToUserAsync(string authorId, CancellationToken ct = default)
    {
        var userAlreadyLinked = await _context.Authors
            .AnyAsync(a => a.UserId == _currentUser.Id, ct);

        if (userAlreadyLinked)
            return Result.Failure(new[] { "Ya tienes un perfil de autor vinculado a tu cuenta." });

        var author = await _context.Authors.FirstOrDefaultAsync(a => a.Id == authorId, ct);
        if (author == null)
            return Result.Failure(new[] { "Autor no encontrado." });

        if (author.UserId != null)
            return Result.Failure(new[] { "Este autor ya está vinculado a otra cuenta." });

        author.UserId = _currentUser.Id;
        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }

    /// <summary>
    /// Searches authors by name. Returns an empty list if the query is shorter than 2 characters.
    /// </summary>
    public async Task<List<AuthorSearchDto>> SearchAsync(string q, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < MinSearchTermLength)
            return new List<AuthorSearchDto>();

        var term = q.Trim().ToLower();

        return await _context.Authors
            .AsNoTracking()
            .Where(a => a.Name.ToLower().Contains(term))
            .OrderBy(a => a.Name)
            .Take(10)
            .Select(a => new AuthorSearchDto(a.Id, a.Name, a.LastName, a.FirstName))
            .ToListAsync(ct);
    }

    /// <summary>
    /// Searches both Author entities and User profiles for co-author assignment.
    /// Results include a 'Type' field indicating the source ('author' or 'user').
    /// </summary>
    public async Task<List<CoauthorSearchDto>> SearchCoauthorsAsync(string q, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < MinSearchTermLength)
            return new List<CoauthorSearchDto>();

        var term = q.Trim().ToLower();

        var authors = await _context.Authors
            .AsNoTracking()
            .Where(a => a.Name.ToLower().Contains(term))
            .OrderBy(a => a.Name)
            .Take(10)
            .Select(a => new CoauthorSearchDto
            {
                Id = a.Id,
                Name = a.Name,
                Type = "author",
                LinkedUser = a.User == null ? null : new LinkedUserSummaryDto
                {
                    Id = a.User.Id,
                    UserName = a.User.UserName,
                    UserLastName1 = a.User.UserLastName1,
                    UserLastName2 = a.User.UserLastName2,
                    Email = a.User.Email,
                    IsTrained = a.User.IsTrained,
                    ScientificCategory = (int)a.User.ScientificCategory,
                    TeachingCategory = (int)a.User.TeachingCategory,
                    InvestigationCategory = (int)a.User.InvestigationCategory,
                    AreaId = a.User.AreaId,
                    AreaNombre = a.User.Area != null ? a.User.Area.Nombre : null,
                    UniversidadId = a.User.Area != null ? a.User.Area.UniversidadId : null,
                    UniversidadNombre = a.User.Area != null && a.User.Area.Universidad != null
                        ? a.User.Area.Universidad.Nombre
                        : null
                }
            })
            .ToListAsync(ct);

        var users = await _context.Users
            .AsNoTracking()
            .Where(u =>
                !_context.Authors.Any(a => a.UserId == u.Id) &&
                (u.UserName.ToLower().Contains(term) ||
                 u.UserLastName1.ToLower().Contains(term) ||
                 (u.UserLastName2 != null && u.UserLastName2.ToLower().Contains(term))))
            .OrderBy(u => u.UserName)
            .Take(10)
            .Select(u => new CoauthorSearchDto
            {
                Id = u.Id,
                Name = (u.UserLastName1 + (u.UserLastName2 != null ? " " + u.UserLastName2 : "") + ", " + u.UserName).Trim(),
                Type = "user",
                LinkedUser = new LinkedUserSummaryDto
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    UserLastName1 = u.UserLastName1,
                    UserLastName2 = u.UserLastName2,
                    Email = u.Email,
                    IsTrained = u.IsTrained,
                    ScientificCategory = (int)u.ScientificCategory,
                    TeachingCategory = (int)u.TeachingCategory,
                    InvestigationCategory = (int)u.InvestigationCategory,
                    AreaId = u.AreaId,
                    AreaNombre = u.Area != null ? u.Area.Nombre : null,
                    UniversidadId = u.Area != null ? u.Area.UniversidadId : null,
                    UniversidadNombre = u.Area != null && u.Area.Universidad != null
                        ? u.Area.Universidad.Nombre
                        : null
                }
            })
            .ToListAsync(ct);

        return authors.Concat(users).ToList();
    }

    public async Task<PotentialAuthorMatchesDto> GetPotentialAuthorMatchesAsync(CancellationToken ct = default)
    {
        var alreadyLinked = await _context.Authors
            .AnyAsync(a => a.UserId == _currentUser.Id, ct);

        if (alreadyLinked)
            return new PotentialAuthorMatchesDto(new List<PotentialAuthorMatchDto>(), new List<PotentialAuthorMatchDto>());

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == _currentUser.Id, ct);

        if (user == null)
            return new PotentialAuthorMatchesDto(new List<PotentialAuthorMatchDto>(), new List<PotentialAuthorMatchDto>());

        var canonicalName = string.IsNullOrEmpty(user.UserLastName2)
            ? $"{user.UserLastName1}, {user.UserName}"
            : $"{user.UserLastName1} {user.UserLastName2}, {user.UserName}";

        // Pre-compute normalized keys from user fields so EF can translate
        // the WHERE clause to SQL without calling TextNormalizer inside LINQ.
        var canonicalKey   = Dashboard_v2.Domain.Common.TextNormalizer.Normalize(canonicalName);
        var firstNameKey   = Dashboard_v2.Domain.Common.TextNormalizer.Normalize(user.UserName);
        var lastName1Key   = Dashboard_v2.Domain.Common.TextNormalizer.Normalize(user.UserLastName1);

        var candidates = await _context.Authors
            .AsNoTracking()
            .Where(a => a.UserId == null
                && a.LastNameKey.Contains(lastName1Key)
                && (a.FirstNameKey != null && a.FirstNameKey.Contains(firstNameKey)
                    || a.SearchKey.Contains(firstNameKey)))
            .Select(a => new { a.Id, a.Name, a.SearchKey })
            .ToListAsync(ct);

        var exact = candidates
            .Where(a => a.SearchKey == canonicalKey)
            .Select(a => new PotentialAuthorMatchDto(a.Id, a.Name))
            .ToList();

        var exactIds = exact.Select(e => e.Id).ToHashSet();

        var fuzzy = candidates
            .Where(a => !exactIds.Contains(a.Id))
            .Select(a => new PotentialAuthorMatchDto(a.Id, a.Name))
            .ToList();

        return new PotentialAuthorMatchesDto(exact, fuzzy);
    }

    /// <summary>
    /// Resolves a list of external author names (from CrossRef/OpenAire) to domain Author entities
    /// using a 4-stage fallback: exact search key → structured match → case-insensitive → partial first-name match.
    /// Unlike <see cref="Common.AuthorResolutionService.ResolveByNameAsync"/>, this method does NOT create new authors;
    /// it returns null matches so the user can confirm or skip each unresolved name.
    /// </summary>
    public async Task<List<ExternalAuthorResolutionDto>> ResolveExternalAuthorsAsync(
        List<string> names, CancellationToken ct = default)
    {
        var result = new List<ExternalAuthorResolutionDto>();

        foreach (var raw in names.Where(n => !string.IsNullOrWhiteSpace(n)))
        {
            var (lastName, firstName) = AuthorNameParser.Parse(raw);
            var searchKey  = Dashboard_v2.Domain.Common.TextNormalizer.Normalize(raw.Trim());
            var lastKey    = Dashboard_v2.Domain.Common.TextNormalizer.Normalize(lastName);

            // Stage 1: Exact normalized search key match
            var match = await _context.Authors
                .AsNoTracking()
                .Include(a => a.User)
                    .ThenInclude(u => u!.Area)
                        .ThenInclude(ar => ar!.Universidad)
                .FirstOrDefaultAsync(a => a.SearchKey == searchKey, ct);

            // Stage 2: Structured last-name + first-name match
            if (match == null && !string.IsNullOrWhiteSpace(firstName))
            {
                var firstKey = Dashboard_v2.Domain.Common.TextNormalizer.Normalize(firstName);
                match = await _context.Authors
                    .AsNoTracking()
                    .Include(a => a.User)
                        .ThenInclude(u => u!.Area)
                            .ThenInclude(ar => ar!.Universidad)
                    .FirstOrDefaultAsync(
                        a => a.LastNameKey == lastKey && a.FirstNameKey == firstKey,
                        ct);
            }

            // Stage 3: Case-insensitive partial match (fallback for rows without normalized keys)
            if (match == null)
            {
                var rawLower = raw.Trim().ToLowerInvariant();
                var candidates = await _context.Authors
                    .AsNoTracking()
                    .Include(a => a.User)
                        .ThenInclude(u => u!.Area)
                            .ThenInclude(ar => ar!.Universidad)
                    .Where(a => a.Name.ToLower() == rawLower)
                    .ToListAsync(ct);
                match = candidates.FirstOrDefault(
                    a => Dashboard_v2.Domain.Common.TextNormalizer.Normalize(a.Name) == searchKey);
            }

            if (match == null && !string.IsNullOrWhiteSpace(firstName))
            {
                var lastNameLower  = lastName.ToLowerInvariant();
                var firstNameLower = firstName.ToLowerInvariant();
                var firstKey       = Dashboard_v2.Domain.Common.TextNormalizer.Normalize(firstName);
                var candidates = await _context.Authors
                    .AsNoTracking()
                    .Include(a => a.User)
                        .ThenInclude(u => u!.Area)
                            .ThenInclude(ar => ar!.Universidad)
                    .Where(a => a.LastName.ToLower() == lastNameLower &&
                                a.FirstName != null && a.FirstName.ToLower() == firstNameLower)
                    .ToListAsync(ct);
                match = candidates.FirstOrDefault(
                    a => Dashboard_v2.Domain.Common.TextNormalizer.Normalize(a.LastName)  == lastKey &&
                         Dashboard_v2.Domain.Common.TextNormalizer.Normalize(a.FirstName) == firstKey);
            }

            ExternalAuthorMatchDto? matchDto = null;
            if (match != null)
            {
                var u = match.User;
                matchDto = new ExternalAuthorMatchDto
                {
                    Id        = match.Id,
                    Name      = match.Name,
                    LastName  = match.LastName,
                    FirstName = match.FirstName,
                    LinkedUser = u == null ? null : new LinkedUserSummaryDto
                    {
                        Id                   = u.Id,
                        UserName             = u.UserName,
                        UserLastName1        = u.UserLastName1,
                        UserLastName2        = u.UserLastName2,
                        Email                = u.Email,
                        IsTrained            = u.IsTrained,
                        ScientificCategory   = (int)u.ScientificCategory,
                        TeachingCategory     = (int)u.TeachingCategory,
                        InvestigationCategory= (int)u.InvestigationCategory,
                        AreaId               = u.AreaId,
                        AreaNombre           = u.Area?.Nombre,
                        UniversidadId        = u.Area?.UniversidadId,
                        UniversidadNombre    = u.Area?.Universidad?.Nombre,
                    }
                };
            }

            result.Add(new ExternalAuthorResolutionDto
            {
                ExternalName = raw.Trim(),
                Match        = matchDto,
            });
        }

        return result;
    }
}
