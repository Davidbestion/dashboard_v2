using Dashboard_v2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dashboard_v2.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<Resource> Resources { get; }
    DbSet<Publication> Publications { get; }
    DbSet<PublicationType> PublicationTypes { get; }
    DbSet<Author> Authors { get; }
    DbSet<AuthorPublication> AuthorPublications { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
