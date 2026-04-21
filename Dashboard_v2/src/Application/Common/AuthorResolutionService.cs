using System.Threading;
using System.Threading.Tasks;
using Dashboard_v2.Application.Common.Interfaces;
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

        author = new Author
        {
            Name = $"{user.UserName} {user.UserLastName1}{(string.IsNullOrEmpty(user.UserLastName2) ? string.Empty : " " + user.UserLastName2)}".Trim(),
            UserId = user.Id
        };

        _context.Authors.Add(author);
        await _context.SaveChangesAsync(cancellationToken);

        return author;
    }
}
