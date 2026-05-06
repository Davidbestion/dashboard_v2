using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Common.Models;
using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dashboard_v2.Application.Publications;

public sealed partial class PublicationService : IPublicationService
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;
    private readonly ICrossRefClient _crossRefClient;
    private readonly IOpenAireClient _openAireClient;
    private readonly IAuthorResolutionService _authorResolution;
    private readonly IAuthorCleanupService _authorCleanup;

    public PublicationService(
        IApplicationDbContext context,
        IUser currentUser,
        ICrossRefClient crossRefClient,
        IOpenAireClient openAireClient,
        IAuthorResolutionService authorResolution,
        IAuthorCleanupService authorCleanup)
    {
        _context = context;
        _currentUser = currentUser;
        _crossRefClient = crossRefClient;
        _openAireClient = openAireClient;
        _authorResolution = authorResolution;
        _authorCleanup = authorCleanup;
    }

    public async Task<(Result Result, string? PublicationId)> CreateAsync(CreatePublicationRequest request, CancellationToken ct = default)
    {
        if (!System.Enum.IsDefined(typeof(Dashboard_v2.Domain.Enums.PublicationType), request.PublicationType))
            return (Result.Failure(new[] { "Tipo de publicación no válido." }), null);

        if (string.IsNullOrWhiteSpace(request.PublishedDate) || !IsValidPartialDate(request.PublishedDate))
            return (Result.Failure(new[] { "La fecha de publicación es obligatoria. Use el formato AAAA, AAAA-MM o AAAA-MM-DD." }), null);

        var dataBase = string.IsNullOrWhiteSpace(request.DataBase) ? null : request.DataBase.Trim();
        var group = request.Group;
        var cuartil = string.IsNullOrWhiteSpace(request.Cuartil) ? null : request.Cuartil?.Trim();

        if (request.PublicationType == Dashboard_v2.Domain.Enums.PublicationType.Diario)
        {
            if (group is null or < 1 or > 4)
                return (Result.Failure(new[] { "El grupo de la revista es obligatorio (1–4)." }), null);

        }
        else if (request.PublicationType != Dashboard_v2.Domain.Enums.PublicationType.Art\u00edculo_de_Divulgaci\u00f3n
                 && request.Index is null or < 1 or > 3)
        {
            return (Result.Failure(new[] { "La indexaci\u00f3n es obligatoria para este tipo de publicaci\u00f3n (1, 2 o 3)." }), null);
        }

        var author = await _authorResolution.GetOrCreateForUserAsync(_currentUser.Id!, ct);
        if (author == null)
            return (Result.Failure(new[] { "Usuario no encontrado." }), null);

        var publication = new Publication
        {
            Title = request.Title.Trim(),
            NormalizedTitle = NormalizeTitle(request.Title),
            PublicationData = request.PublicationData,
            PublicationType = request.PublicationType,
            PublishedDate = request.PublishedDate.Trim(),
            UrlDoi = string.IsNullOrWhiteSpace(request.UrlDoi) ? null : request.UrlDoi.Trim(),
            NormalizedUrlDoi = string.IsNullOrWhiteSpace(request.UrlDoi) ? null : NormalizeUrlDoi(request.UrlDoi),
            ProyectoId = string.IsNullOrWhiteSpace(request.ProyectoId) ? null : request.ProyectoId,
            AuthorPublications = new List<AuthorPublication> { new() { AuthorId = author.Id } }
        };

        await AddCoauthorsAsync(publication, author, request.AdditionalAuthorIds, request.AdditionalAuthorNames, request.AdditionalUserIds, ct);

        if (request.PublicationType == Dashboard_v2.Domain.Enums.PublicationType.Diario)
        {
            publication.JournalPublication = new JournalPublication
            {
                PublicationId = publication.Id,
                DataBase = dataBase,
                Group = group!.Value,
                JournalGroup1Publication = group == 1
                    ? new JournalGroup1Publication { PublicationId = publication.Id, Cuartil = cuartil }
                    : null
            };
        }
        else
        {
            publication.IndexedPublication = new IndexedPublication
            {
                PublicationId = publication.Id,
                Index = request.Index
            };
        }

        _context.Publications.Add(publication);
        await _context.SaveChangesAsync(ct);

        return (Result.Success(), publication.Id);
    }

    public async Task<Result> UpdateAsync(UpdatePublicationRequest request, CancellationToken ct = default)
    {
        var currentAuthor = await _authorResolution.GetOrCreateForUserAsync(_currentUser.Id!, ct);
        if (currentAuthor == null)
            return Result.Failure(new[] { "Publicación no encontrada o no tienes permiso para editarla." });

        var isAuthor = await _context.AuthorPublications
            .AnyAsync(ap => ap.PublicationId == request.Id && ap.AuthorId == currentAuthor.Id, ct);

        if (!isAuthor)
            return Result.Failure(new[] { "Publicación no encontrada o no tienes permiso para editarla." });

        var publication = await _context.Publications
            .Include(p => p.AuthorPublications)
            .Include(p => p.JournalPublication)
                .ThenInclude(jp => jp!.JournalGroup1Publication)
            .Include(p => p.IndexedPublication)
            .FirstOrDefaultAsync(p => p.Id == request.Id, ct);

        if (publication == null)
            return Result.Failure(new[] { "Publicación no encontrada." });

        if (!System.Enum.IsDefined(typeof(Dashboard_v2.Domain.Enums.PublicationType), request.PublicationType))
            return Result.Failure(new[] { "Tipo de publicación no válido." });

        if (string.IsNullOrWhiteSpace(request.PublishedDate) || !IsValidPartialDate(request.PublishedDate))
            return Result.Failure(new[] { "La fecha de publicación es obligatoria. Use el formato AAAA, AAAA-MM o AAAA-MM-DD." });

        publication.Title = request.Title.Trim();
        publication.NormalizedTitle = NormalizeTitle(request.Title);
        publication.PublicationData = request.PublicationData;
        publication.PublicationType = request.PublicationType;
        publication.PublishedDate = request.PublishedDate.Trim();
        publication.UrlDoi = string.IsNullOrWhiteSpace(request.UrlDoi) ? null : request.UrlDoi.Trim();
        publication.NormalizedUrlDoi = string.IsNullOrWhiteSpace(request.UrlDoi) ? null : NormalizeUrlDoi(request.UrlDoi);
        publication.ProyectoId = string.IsNullOrWhiteSpace(request.ProyectoId) ? null : request.ProyectoId;

        var dataBase = string.IsNullOrWhiteSpace(request.DataBase) ? null : request.DataBase.Trim();
        var group    = request.Group;
        var cuartil  = string.IsNullOrWhiteSpace(request.Cuartil) ? null : request.Cuartil?.Trim();

        var isNowJournal = request.PublicationType == Dashboard_v2.Domain.Enums.PublicationType.Diario;
        var wasJournal = publication.JournalPublication != null;

        if (isNowJournal)
        {
            if (string.IsNullOrWhiteSpace(dataBase) || group is null or < 1 or > 4)
                return Result.Failure(new[] { "Datos de la revista son obligatorios: base de datos y grupo (1–4)." });
        }
        else if (request.PublicationType != Dashboard_v2.Domain.Enums.PublicationType.Art\u00edculo_de_Divulgaci\u00f3n
                 && request.Index is null or < 1 or > 3)
        {
            return Result.Failure(new[] { "La indexaci\u00f3n es obligatoria para este tipo de publicaci\u00f3n (1, 2 o 3)." });
        }

        if (wasJournal && !isNowJournal)
        {
            if (publication.JournalPublication!.JournalGroup1Publication != null)
                _context.JournalGroup1Publications.Remove(publication.JournalPublication.JournalGroup1Publication);
            _context.JournalPublications.Remove(publication.JournalPublication);
            publication.JournalPublication = null;
        }
        else if (!wasJournal && isNowJournal && publication.IndexedPublication != null)
        {
            _context.IndexedPublications.Remove(publication.IndexedPublication);
            publication.IndexedPublication = null;
        }

        if (isNowJournal)
        {
            if (publication.JournalPublication == null)
            {
                publication.JournalPublication = new JournalPublication
                {
                    PublicationId = publication.Id,
                    DataBase = dataBase,
                    Group = group!.Value
                };
            }
            else
            {
                publication.JournalPublication.DataBase = dataBase;
                publication.JournalPublication.Group = group!.Value;
            }

            if (group == 1)
            {
                if (publication.JournalPublication.JournalGroup1Publication == null)
                {
                    var g1 = new JournalGroup1Publication { PublicationId = publication.Id, Cuartil = cuartil };
                    publication.JournalPublication.JournalGroup1Publication = g1;
                    _context.JournalGroup1Publications.Add(g1);
                }
                else
                {
                    publication.JournalPublication.JournalGroup1Publication.Cuartil = cuartil;
                }
            }
            else if (publication.JournalPublication.JournalGroup1Publication != null)
            {
                _context.JournalGroup1Publications.Remove(publication.JournalPublication.JournalGroup1Publication);
                publication.JournalPublication.JournalGroup1Publication = null;
            }
        }
        else
        {
            if (publication.IndexedPublication == null)
            {
                var indexed = new IndexedPublication { PublicationId = publication.Id, Index = request.Index };
                publication.IndexedPublication = indexed;
                _context.IndexedPublications.Add(indexed);
            }
            else
            {
                publication.IndexedPublication.Index = request.Index;
            }
        }

        var removedAuthorIds = publication.AuthorPublications
            .Where(ap => ap.AuthorId != currentAuthor.Id)
            .Select(ap => ap.AuthorId)
            .ToList();
        foreach (var authorIdToRemove in removedAuthorIds)
        {
            var ap = publication.AuthorPublications.First(x => x.AuthorId == authorIdToRemove);
            publication.AuthorPublications.Remove(ap);
        }

        await AddCoauthorsAsync(publication, currentAuthor, request.AdditionalAuthorIds, request.AdditionalAuthorNames, request.AdditionalUserIds, ct);

        await _context.SaveChangesAsync(ct);

        await _authorCleanup.CleanupIfOrphanedAsync(removedAuthorIds, ct);

        return Result.Success();
    }

    // ── Static compiled regexes — compiled once per AppDomain, reused on every call. ─────
    [System.Text.RegularExpressions.GeneratedRegex(@"^\d{4}$")]
    private static partial System.Text.RegularExpressions.Regex YearOnlyRegex();

    [System.Text.RegularExpressions.GeneratedRegex(@"^\d{4}-(0[1-9]|1[0-2])$")]
    private static partial System.Text.RegularExpressions.Regex YearMonthRegex();

    /// <summary>
    /// Validates a partial ISO date string: "YYYY", "YYYY-MM", or "YYYY-MM-DD".
    /// </summary>
    private static bool IsValidPartialDate(string value)
    {
        var v = value.Trim();
        if (YearOnlyRegex().IsMatch(v)) return true;
        if (YearMonthRegex().IsMatch(v)) return true;
        return System.DateOnly.TryParseExact(v, "yyyy-MM-dd",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out _);
    }

    /// <summary>
    /// Adds coauthors to <paramref name="publication"/> from the three coauthor source lists.
    /// Guarantees no duplicate <see cref="AuthorPublication"/> entries are added.
    /// </summary>
    private async Task AddCoauthorsAsync(
        Publication publication,
        Author primaryAuthor,
        IEnumerable<string> additionalAuthorIds,
        IEnumerable<string> additionalAuthorNames,
        IEnumerable<string> additionalUserIds,
        CancellationToken ct)
    {
        foreach (var authorId in additionalAuthorIds.Where(id => !string.IsNullOrWhiteSpace(id)))
        {
            if (authorId != primaryAuthor.Id
                && publication.AuthorPublications.All(ap => ap.AuthorId != authorId)
                && await _context.Authors.AnyAsync(a => a.Id == authorId, ct))
            {
                publication.AuthorPublications.Add(new AuthorPublication { AuthorId = authorId });
            }
        }

        foreach (var name in additionalAuthorNames.Where(n => !string.IsNullOrWhiteSpace(n)))
        {
            var resolved = await _authorResolution.ResolveByNameAsync(name, ct);
            if (publication.AuthorPublications.All(ap => ap.AuthorId != resolved.Id))
                publication.AuthorPublications.Add(new AuthorPublication { AuthorId = resolved.Id });
        }

        foreach (var userId in additionalUserIds.Where(id => !string.IsNullOrWhiteSpace(id)))
        {
            if (userId == _currentUser.Id) continue;
            var coAuthor = await _authorResolution.GetOrCreateForUserAsync(userId, ct);
            if (coAuthor == null) continue;
            if (publication.AuthorPublications.All(ap => ap.AuthorId != coAuthor.Id))
                publication.AuthorPublications.Add(new AuthorPublication { AuthorId = coAuthor.Id });
        }
    }

    private static string NormalizeTitle(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return string.Empty;
        s = s.Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder();
        foreach (var ch in s)
        {
            var cat = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
            if (cat != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }
        s = sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
        s = System.Text.RegularExpressions.Regex.Replace(s, @"\p{P}", "");
        s = System.Text.RegularExpressions.Regex.Replace(s, @"\s+", " ").Trim().ToLowerInvariant();
        return s;
    }

    private static string NormalizeUrlDoi(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return string.Empty;
        s = s.Trim().ToLowerInvariant();
        // remove scheme
        s = System.Text.RegularExpressions.Regex.Replace(s, "^https?://", "");
        // remove common doi host prefixes
        s = s.Replace("doi.org/", "").Replace("dx.doi.org/", "").Replace("doi:", "");
        // remove trailing slashes and params
        var idx = s.IndexOf('?');
        if (idx >= 0) s = s.Substring(0, idx);
        s = s.Trim().TrimEnd('/');
        return s;
    }

    public async Task<List<PublicationDuplicateDto>> FindDuplicatesAsync(string? title, string? doi, string? url, string? excludePublicationId = null, CancellationToken ct = default)
    {
        var normalizedTitle = string.IsNullOrWhiteSpace(title) ? null : NormalizeTitle(title);
        var inputUrlDoi = string.IsNullOrWhiteSpace(doi) ? (string.IsNullOrWhiteSpace(url) ? null : NormalizeUrlDoi(url)) : NormalizeUrlDoi(doi);

        var candidates = new List<PublicationDuplicateDto>();

        if (!string.IsNullOrWhiteSpace(inputUrlDoi))
        {
            // Find by normalized doi or by UrlDoi containing the input DOI (covers legacy rows)
            var matchesRaw = await _context.Publications
                .AsNoTracking()
                .Where(p => ((p.NormalizedUrlDoi != null && p.NormalizedUrlDoi == inputUrlDoi)
                         || (p.UrlDoi != null && p.UrlDoi.ToLower().Contains(inputUrlDoi)))
                         && (excludePublicationId == null || p.Id != excludePublicationId))
                .Select(p => new { p.Id, p.Title, p.UrlDoi, p.NormalizedUrlDoi })
                .ToListAsync(ct);

            foreach (var p in matchesRaw)
            {
                candidates.Add(new PublicationDuplicateDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    UrlDoi = p.UrlDoi,
                    MatchType = "doi",
                    Score = 1.0
                });
            }

        }

        if (!string.IsNullOrWhiteSpace(normalizedTitle))
        {
            var matches = await _context.Publications
                .AsNoTracking()
                .Where(p => p.NormalizedTitle != null && p.NormalizedTitle == normalizedTitle && (excludePublicationId == null || p.Id != excludePublicationId))
                .Select(p => new PublicationDuplicateDto { Id = p.Id, Title = p.Title, UrlDoi = p.UrlDoi, MatchType = "title", Score = 0.95 })
                .ToListAsync(ct);

            // avoid duplicates
            foreach (var m in matches)
                if (!candidates.Any(c => c.Id == m.Id)) candidates.Add(m);
        }

        return candidates.OrderByDescending(c => c.Score).ToList();
    }

    public async Task<Result> AddCurrentUserAsCoauthorAsync(string publicationId, CancellationToken ct = default)
    {
        var author = await _authorResolution.GetOrCreateForUserAsync(_currentUser.Id!, ct);
        if (author == null) return Result.Failure(new[] { "Usuario no encontrado." });

        var exists = await _context.AuthorPublications
            .AnyAsync(ap => ap.PublicationId == publicationId && ap.AuthorId == author.Id, ct);

        if (exists) return Result.Success();

        // ensure publication exists
        var publication = await _context.Publications.FindAsync(new object[] { publicationId }, ct);
        if (publication == null) return Result.Failure(new[] { "Publicación no encontrada." });

        _context.AuthorPublications.Add(new Domain.Entities.AuthorPublication { PublicationId = publicationId, AuthorId = author.Id });
        await _context.SaveChangesAsync(ct);

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(string id, CancellationToken ct = default)
    {
        var isAuthor = await _context.AuthorPublications
            .AnyAsync(ap => ap.PublicationId == id && ap.Author.UserId == _currentUser.Id, ct);

        if (!isAuthor)
            return Result.Failure(new[] { "Publicación no encontrada o no tienes permiso para eliminarla." });

        var publication = await _context.Publications
            .Include(p => p.AuthorPublications)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (publication == null)
            return Result.Failure(new[] { "Publicación no encontrada." });

        var authorIds = publication.AuthorPublications.Select(ap => ap.AuthorId).ToList();

        _context.Publications.Remove(publication);
        await _context.SaveChangesAsync(ct);

        await _authorCleanup.CleanupIfOrphanedAsync(authorIds, ct);

        return Result.Success();
    }

    public async Task<PublicationDto?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var authorId = await _context.Authors
            .AsNoTracking()
            .Where(a => a.UserId == _currentUser.Id)
            .Select(a => a.Id)
            .FirstOrDefaultAsync(ct);

        if (authorId == null)
            return null;

        return await ProjectPublicationDtos(
            _context.Publications.AsNoTracking()
            .Where(p => p.Id == id && p.AuthorPublications.Any(ap => ap.AuthorId == authorId))
            )
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>
    /// Obtener una publicación por Id sin restringir a que el usuario sea autor.
    /// Usado para mostrar detalles cuando se detecta un duplicado.
    /// </summary>
    public async Task<PublicationDto?> GetPublicByIdAsync(string id, CancellationToken ct = default)
    {
        return await ProjectPublicationDtos(
            _context.Publications.AsNoTracking().Where(p => p.Id == id)
        ).FirstOrDefaultAsync(ct);
    }

    public async Task<List<PublicationDto>> GetMyPublicationsAsync(CancellationToken ct = default)
    {
        var authorId = await _context.Authors
            .AsNoTracking()
            .Where(a => a.UserId == _currentUser.Id)
            .Select(a => a.Id)
            .FirstOrDefaultAsync(ct);

        if (authorId == null)
            return new List<PublicationDto>();

        return await ProjectPublicationDtos(
            _context.Publications.AsNoTracking()
            .Where(p => p.AuthorPublications.Any(ap => ap.AuthorId == authorId))
            )
            .OrderBy(p => p.Title)
            .ToListAsync(ct);
    }

    public async Task<List<PublicationDto>> GetAllPublicationsAsync(CancellationToken ct = default)
    {
        return await ProjectPublicationDtos(_context.Publications.AsNoTracking())
            .OrderBy(p => p.Title)
            .ToListAsync(ct);
    }

    public async Task<List<PublicationDto>> GetAreaPublicationsAsync(CancellationToken ct = default)
    {
        var areaId = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == _currentUser.Id)
            .Select(u => u.AreaId)
            .FirstOrDefaultAsync(ct);

        if (areaId == null)
            return new List<PublicationDto>();

        return await ProjectPublicationDtos(
            _context.Publications.AsNoTracking()
                .Where(p => p.AuthorPublications.Any(ap =>
                    ap.Author.UserId != null &&
                    ap.Author.User!.AreaId == areaId)))
            .OrderBy(p => p.Title)
            .ToListAsync(ct);
    }

    public async Task<List<PublicationCrossRefDto>> SearchCrossRefCandidatesAsync(string? doi, string? title, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(doi))
        {
            var single = await _crossRefClient.GetWorkByDoiAsync(doi, ct);
            if (single is not null)
                return new List<PublicationCrossRefDto> { EnrichCrossRefCandidate(single) };
        }

        if (!string.IsNullOrWhiteSpace(title))
        {
            var items = await _crossRefClient.SearchWorksByTitleAsync(title, rows: 10, ct);
            return items.Select(EnrichCrossRefCandidate).ToList();
        }

        return new List<PublicationCrossRefDto>();
    }

    public async Task<List<PublicationCrossRefDto>> SearchOpenAireCandidatesAsync(string? doi, string? title, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(doi))
        {
            var single = await _openAireClient.GetWorkByDoiAsync(doi, ct);
            if (single is not null)
                return new List<PublicationCrossRefDto> { EnrichCrossRefCandidate(single) };
        }

        if (!string.IsNullOrWhiteSpace(title))
        {
            var items = await _openAireClient.SearchWorksByTitleAsync(title, rows: 10, ct);
            return items.Select(EnrichCrossRefCandidate).ToList();
        }

        return new List<PublicationCrossRefDto>();
    }

    /// <summary>
    /// Proyección reutilizable de publicaciones hacia el DTO usado por las vistas.
    /// Centraliza la forma en que se exponen autores, metadatos de revista,
    /// datos de indexación y proyecto vinculado.
    /// </summary>
    /// <param name="query">Consulta base sobre publicaciones.</param>
    /// <returns>Consulta proyectada a <see cref="PublicationDto"/>.</returns>
    private static IQueryable<PublicationDto> ProjectPublicationDtos(IQueryable<Publication> query)
    {
        return query.Select(p => new PublicationDto
        {
            Id = p.Id,
            Title = p.Title,
            PublicationData = p.PublicationData,
            UrlDoi = p.UrlDoi,
            PublishedDate = p.PublishedDate,
            PublicationType = (int)p.PublicationType,
            Authors = p.AuthorPublications
                .Select(ap => new AuthorDto
                {
                    Id = ap.Author.Id,
                    Name = ap.Author.Name,
                    LastName = ap.Author.LastName,
                    FirstName = ap.Author.FirstName,
                    UserId = ap.Author.UserId,
                    LinkedUser = ap.Author.User == null ? null : new LinkedUserSummaryDto
                    {
                        Id = ap.Author.User.Id,
                        UserName = ap.Author.User.UserName,
                        UserLastName1 = ap.Author.User.UserLastName1,
                        UserLastName2 = ap.Author.User.UserLastName2,
                        Email = ap.Author.User.Email,
                        IsTrained = ap.Author.User.IsTrained,
                        ScientificCategory = (int)ap.Author.User.ScientificCategory,
                        TeachingCategory = (int)ap.Author.User.TeachingCategory,
                        InvestigationCategory = (int)ap.Author.User.InvestigationCategory,
                        AreaId = ap.Author.User.AreaId,
                        AreaNombre = ap.Author.User.Area != null ? ap.Author.User.Area.Nombre : null,
                        UniversidadId = ap.Author.User.Area != null ? ap.Author.User.Area.UniversidadId : null,
                        UniversidadNombre = ap.Author.User.Area != null && ap.Author.User.Area.Universidad != null
                            ? ap.Author.User.Area.Universidad.Nombre
                            : null
                    }
                })
                .ToList(),
            IndexedPublication = p.IndexedPublication == null ? null : new IndexedPublicationDto
            {
                Index = p.IndexedPublication.Index
            },
            JournalPublication = p.JournalPublication == null ? null : new JournalPublicationDto
            {
                DataBase = p.JournalPublication.DataBase,
                Group = p.JournalPublication.Group,
                Cuartil = p.JournalPublication.JournalGroup1Publication != null
                    ? p.JournalPublication.JournalGroup1Publication.Cuartil
                    : null
            },
            ProyectoId = p.ProyectoId,
            ProyectoTitulo = p.Proyecto != null ? p.Proyecto.Titulo : null
        });
    }

    public Task<List<PublicationTypeDto>> GetPublicationTypesAsync()
    {
        var types = System.Enum.GetValues<Dashboard_v2.Domain.Enums.PublicationType>()
            .Select(t => new PublicationTypeDto((int)t, t.ToString().Replace('_', ' ')))
            .ToList();

        return Task.FromResult(types);
    }

    private static PublicationCrossRefDto EnrichCrossRefCandidate(PublicationCrossRefDto dto)
    {
        var publicationType = CrossRefToPublicationMapper.MapCrossRefTypeToPublicationType(dto.Type);
        dto.PublicationData = CrossRefToPublicationMapper.BuildPublicationData(dto);
        dto.SuggestedPublicationType = publicationType is null ? null : (int)publicationType.Value;
        return dto;
    }
}
