namespace Dashboard_v2.Application.Clasificaciones;

public record CreateClasificacionRequest
{
    public string Nombre { get; init; } = default!;
}

public record UpdateClasificacionRequest
{
    public string Nombre { get; init; } = default!;
}
