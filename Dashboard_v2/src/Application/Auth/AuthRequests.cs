using Dashboard_v2.Domain.Entities;

namespace Dashboard_v2.Application.Auth;

/// <summary>
/// Datos necesarios para registrar un nuevo usuario mediante el flujo CRUD de autenticación.
/// </summary>
public sealed record RegisterRequest
{
    public string UserName { get; init; } = default!;
    public string UserLastName1 { get; init; } = default!;
    public string? UserLastName2 { get; init; }
    public string Email { get; init; } = default!;
    public string Password { get; init; } = default!;
    public DateTime BirthDate { get; init; }
    public bool IsTrained { get; init; }
    public TeachingCategory TeachingCategory { get; init; } = TeachingCategory.None;
    public ScientificCategory ScientificCategory { get; init; } = ScientificCategory.None;
    public InvestigationCategory InvestigationCategory { get; init; } = InvestigationCategory.None;
}

/// <summary>
/// Datos de entrada para autenticar a un usuario.
/// </summary>
public sealed record LoginRequest
{
    public string Email { get; init; } = default!;
    public string Password { get; init; } = default!;
    public string? SelectedRole { get; init; }
    public string? SelectedAreaId { get; init; }
}
