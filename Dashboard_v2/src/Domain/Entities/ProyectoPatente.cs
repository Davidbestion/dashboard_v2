namespace Dashboard_v2.Domain.Entities;

/// <summary>
/// Tabla de unión entre <see cref="Proyecto"/> y <see cref="Patente"/>.
/// Representa la relación N:M "una patente puede ser resultado de varios proyectos;
/// un proyecto puede generar varias patentes".
/// </summary>
public class ProyectoPatente
{
    public string ProyectoId { get; set; } = default!;
    public Proyecto Proyecto { get; set; } = default!;

    public string PatenteId { get; set; } = default!;
    public Patente Patente { get; set; } = default!;
}
