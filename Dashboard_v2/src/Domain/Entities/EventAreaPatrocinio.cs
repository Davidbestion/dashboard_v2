namespace Dashboard_v2.Domain.Entities;

/// <summary>
/// Entidad de unión explícita para la relación (0,*) ↔ (0,*) entre Area y Event
/// que representa el patrocinio/auspicio de un Area sobre un Evento.
/// </summary>
public class EventAreaPatrocinio
{
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;

    public string AreaId { get; set; } = default!;
    public Area Area { get; set; } = null!;
}
