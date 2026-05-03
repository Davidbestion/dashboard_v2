namespace Dashboard_v2.Application.Auth;

/// <summary>
/// Datos mínimos del usuario autenticado que consume el frontend para construir la sesión.
/// </summary>
public sealed record CurrentUserDto
{
    public string Id { get; init; } = default!;
    public string UserName { get; init; } = default!;
    public string Email { get; init; } = default!;
    public string? Role { get; init; }
    public string? AreaId { get; init; }
    public string? AreaNombre { get; init; }
    /// <summary>True si el usuario ya tiene una entidad Author vinculada en el sistema.</summary>
    public bool HasLinkedAuthor { get; init; }
}
