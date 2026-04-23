using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace Dashboard_v2.Infrastructure.Identity;

/// <summary>
/// Encapsula la lógica necesaria para garantizar que un usuario tenga un área asignada
/// antes de que el proceso de autenticación emita el token final de sesión.
/// </summary>
public sealed class UserAreaResolutionService
{
    private readonly IApplicationDbContext _context;

    /// <summary>
    /// Inicializa el servicio con acceso al contexto de aplicación para consultar y persistir el área del usuario.
    /// </summary>
    public UserAreaResolutionService(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Garantiza que el usuario indicado tenga un área persistida.
    /// Si todavía no la tiene y el cliente no envió una selección, devuelve las áreas disponibles
    /// para que la UI muestre el paso de selección.
    /// </summary>
    public async Task<(Result Result, LoginResponse? Response)> EnsureAreaAssignedAsync(
        string userId,
        string? selectedAreaId,
        CancellationToken ct = default)
    {
        var userAreaState = await _context.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new { user.Id, user.AreaId })
            .FirstOrDefaultAsync(ct);

        if (userAreaState is null)
        {
            return (Result.Failure(["No fue posible resolver el usuario autenticado."]), null);
        }

        if (!string.IsNullOrWhiteSpace(userAreaState.AreaId))
        {
            return (Result.Success(), new LoginResponse());
        }

        if (string.IsNullOrWhiteSpace(selectedAreaId))
        {
            return await BuildAreaSelectionResponseAsync(ct);
        }

        var normalizedAreaId = selectedAreaId.Trim();

        var selectedAreaExists = await _context.Areas
            .AsNoTracking()
            .AnyAsync(area => area.Id == normalizedAreaId, ct);

        if (!selectedAreaExists)
        {
            return (Result.Failure(["El área seleccionada no es válida."]), null);
        }

        var userToUpdate = await _context.Users.FirstOrDefaultAsync(user => user.Id == userId, ct);
        if (userToUpdate is null)
        {
            return (Result.Failure(["No fue posible resolver el usuario autenticado."]), null);
        }

        // La comprobación se repite sobre la entidad trackeada para evitar sobrescribir un área
        // si otra petición concurrente ya la persistió entre la lectura inicial y este punto.
        if (string.IsNullOrWhiteSpace(userToUpdate.AreaId))
        {
            userToUpdate.AreaId = normalizedAreaId;
            await _context.SaveChangesAsync(ct);
        }

        return (Result.Success(), new LoginResponse());
    }

    /// <summary>
    /// Construye la respuesta que obliga al cliente a seleccionar un área.
    /// Si el sistema aún no tiene áreas registradas, devuelve un error explicativo
    /// para evitar que la UI quede en un paso sin opciones válidas.
    /// </summary>
    private async Task<(Result Result, LoginResponse? Response)> BuildAreaSelectionResponseAsync(CancellationToken ct)
    {
        var availableAreas = await _context.Areas
            .AsNoTracking()
            .OrderBy(area => area.Nombre)
            .Select(area => new AreaOptionDto
            {
                Id = area.Id,
                Nombre = area.Nombre
            })
            .ToListAsync(ct);

        if (availableAreas.Count == 0)
        {
            return (Result.Failure(["No existen áreas registradas. Contacta a un administrador antes de iniciar sesión."]), null);
        }

        return (Result.Success(), new LoginResponse
        {
            RequiresAreaSelection = true,
            AvailableAreas = availableAreas
        });
    }
}
