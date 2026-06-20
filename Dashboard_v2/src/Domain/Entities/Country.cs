namespace Dashboard_v2.Domain.Entities;

public class Country
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;

    public ICollection<Event> Events { get; set; } = new List<Event>();
    public ICollection<Registro> Registros { get; set; } = new List<Registro>();
    public ICollection<Red> Reds { get; set; } = new List<Red>();
}