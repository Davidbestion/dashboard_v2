using System.Threading;
using System.Threading.Tasks;
using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Domain.Common;
using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dashboard_v2.Application.Common;

public sealed class AuthorResolutionService : IAuthorResolutionService
{
    private readonly IApplicationDbContext _context;

    public AuthorResolutionService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Author?> GetOrCreateForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var author = await _context.Authors
            .FirstOrDefaultAsync(a => a.UserId == userId, cancellationToken);

        if (author != null)
            return author;

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
            return null;

        author = Author.Create(
            lastName : (user.UserLastName1 + (string.IsNullOrEmpty(user.UserLastName2) ? string.Empty : " " + user.UserLastName2)).Trim(),
            firstName: user.UserName.Trim());
        author.UserId = user.Id;

        _context.Authors.Add(author);
        await _context.SaveChangesAsync(cancellationToken);

        return author;
    }

    public async Task<Author> ResolveByNameAsync(string nameString, CancellationToken cancellationToken = default)
    {
        var (lastName, firstName) = AuthorNameParser.Parse(nameString);
        var searchKey = TextNormalizer.Normalize(nameString.Trim());
        var lastKey   = TextNormalizer.Normalize(lastName);

        // 1. Match on the normalized search key (tolerates diacritics & case).
        var existing = await _context.Authors
            .FirstOrDefaultAsync(a => a.SearchKey == searchKey, cancellationToken);

        if (existing != null)
            return existing;

        // 2. Structured match: normalized LastName + normalized FirstName (both non-empty).
        if (existing == null && !string.IsNullOrWhiteSpace(firstName))
        {
            var firstKey = TextNormalizer.Normalize(firstName);
            existing = await _context.Authors
                .FirstOrDefaultAsync(
                    a => a.LastNameKey == lastKey && a.FirstNameKey == firstKey,
                    cancellationToken);
        }

        // 3. Fallback for rows whose SearchKey was backfilled with SQL lower() (no accent
        //    stripping).  Compare on the raw Name column case-insensitively — both sides
        //    retain the original accents so the comparison succeeds — then confirm with
        //    in-memory normalization.  Self-heals the stored key on match.
        if (existing == null)
        {
            var rawLower = nameString.Trim().ToLowerInvariant();
            var candidates = await _context.Authors
                .Where(a => a.Name.ToLower() == rawLower)
                .ToListAsync(cancellationToken);
            existing = candidates.FirstOrDefault(
                a => TextNormalizer.Normalize(a.Name) == searchKey);
        }

        if (existing == null && !string.IsNullOrWhiteSpace(firstName))
        {
            var lastNameLower  = lastName.ToLowerInvariant();
            var firstNameLower = firstName.ToLowerInvariant();
            var firstKey       = TextNormalizer.Normalize(firstName);
            var candidates = await _context.Authors
                .Where(a => a.LastName.ToLower() == lastNameLower &&
                            a.FirstName != null && a.FirstName.ToLower() == firstNameLower)
                .ToListAsync(cancellationToken);
            existing = candidates.FirstOrDefault(
                a => TextNormalizer.Normalize(a.LastName)  == lastKey &&
                     TextNormalizer.Normalize(a.FirstName) == firstKey);
        }

        if (existing != null)
        {
            // Self-heal: if the stored keys are stale (from SQL backfill), recompute them.
            var correctSearchKey = TextNormalizer.Normalize(existing.Name);
            if (existing.SearchKey != correctSearchKey)
            {
                existing.SearchKey   = correctSearchKey;
                existing.LastNameKey = TextNormalizer.Normalize(existing.LastName);
                existing.FirstNameKey = string.IsNullOrWhiteSpace(existing.FirstName)
                    ? null
                    : TextNormalizer.Normalize(existing.FirstName);
                await _context.SaveChangesAsync(cancellationToken);
            }
            return existing;
        }

        // 4. No match — create and persist a new author.
        var author = Author.Create(lastName, firstName);
        _context.Authors.Add(author);
        await _context.SaveChangesAsync(cancellationToken);
        return author;
    }
}
