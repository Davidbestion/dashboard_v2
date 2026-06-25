namespace Dashboard_v2.Application.Clasificaciones;

/// <summary>Request to create a new project classification nomenclator entry.</summary>
public record CreateClasificacionRequest
{
    public string Nombre { get; init; } = default!;
}

/// <summary>Request to rename an existing project classification entry.</summary>
public record UpdateClasificacionRequest
{
    public string Nombre { get; init; } = default!;
}
