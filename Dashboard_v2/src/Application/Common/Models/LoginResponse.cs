namespace Dashboard_v2.Application.Common.Models;

/// <summary>
/// Response returned after a login attempt. Contains either the JWT token (single-role users)
/// or lists of available roles/areas from which the client must choose before a token is issued.
/// </summary>
public record LoginResponse
{
    public bool RequiresRoleSelection { get; init; }
    public IEnumerable<string> AvailableRoles { get; init; } = [];
    public bool RequiresAreaSelection { get; init; }
    public IEnumerable<AreaOptionDto> AvailableAreas { get; init; } = [];
    public string? Token { get; init; }
}

/// <summary>Minimal area entry returned during role/area selection on login.</summary>
public record AreaOptionDto
{
    public string Id { get; init; } = default!;
    public string Nombre { get; init; } = default!;
}
