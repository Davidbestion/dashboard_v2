namespace Dashboard_v2.Application.Users;

/// <summary>DTO de respuesta que combina datos del usuario con sus roles como strings (para JSON).</summary>
public record UserWithRolesDto
{
    public string Id { get; init; } = default!;
    public string UserName { get; init; } = default!;
    public string UserLastName1 { get; init; } = default!;
    public string? UserLastName2 { get; init; }
    public string Email { get; init; } = default!;
    public bool IsActive { get; init; }
    public bool IsTrained { get; init; }
    public int ScientificCategory { get; init; }
    public int TeachingCategory { get; init; }
    public int InvestigationCategory { get; init; }
    public string? AreaId { get; init; }
    public string? AreaNombre { get; init; }
    public string? UniversidadId { get; init; }
    public string? UniversidadNombre { get; init; }
    public List<string> Roles { get; init; } = new();
}

/// <summary>DTO reducido para seleccionar un Jefe de Proyecto en el formulario de proyectos.</summary>
public record JefeDeProyectoDto
{
    public string Id { get; init; } = default!;
    public string NombreCompleto { get; init; } = default!;
    public string Email { get; init; } = default!;
}
