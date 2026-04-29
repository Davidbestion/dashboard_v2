namespace Dashboard_v2.Domain.Entities;

public class RedCoordinada
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    // Relación con Red
    public string RedId { get; set; } = default!;
    public Red Red { get; set; } = default!;

    // Relación con Area
    public string AreaId { get; set; } = default!;
    public Area Area { get; set; } = default!;

    // Coordinador (usuario encargado de coordinar la red en el área)
    public string CoordinadorId { get; set; } = default!;
    public User Coordinador { get; set; } = default!;
}
