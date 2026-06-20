namespace Dashboard_v2.Application.Roles;

/// <summary>DTO de respuesta con el nombre de un rol del sistema (para serializar a JSON).</summary>
public record RoleDto
{
    public string Name { get; init; } = default!;
}
