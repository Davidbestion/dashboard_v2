namespace Dashboard_v2.Domain.Entities;

public class PublicationType
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = default!;
}
