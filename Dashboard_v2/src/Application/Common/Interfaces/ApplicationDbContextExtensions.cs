using Microsoft.EntityFrameworkCore;

namespace Dashboard_v2.Application.Common.Interfaces;

/// <summary>
/// Operaciones de consulta reutilizables sobre <see cref="IApplicationDbContext"/>
/// que no justifican un servicio dedicado.
/// </summary>
public static class ApplicationDbContextExtensions
{
    /// <summary>
    /// Resuelve el área académica del usuario indicado.
    /// Devuelve <c>null</c> si el usuario no existe o no tiene área asignada.
    /// </summary>
    public static Task<string?> GetUserAreaIdAsync(
        this IApplicationDbContext context, string? userId, CancellationToken ct = default)
        => context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.AreaId)
            .FirstOrDefaultAsync(ct);
}
