namespace Dashboard_v2.Application.Common.Models;

public record LoginResponse
{
    public bool RequiresRoleSelection { get; init; }
    public IEnumerable<string> AvailableRoles { get; init; } = [];
    public bool RequiresAreaSelection { get; init; }
    public IEnumerable<AreaOptionDto> AvailableAreas { get; init; } = [];
    public string? Token { get; init; }
}

public record AreaOptionDto
{
    public string Id { get; init; } = default!;
    public string Nombre { get; init; } = default!;
}
