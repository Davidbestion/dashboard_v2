namespace Dashboard_v2.Application.Common.Models;

public class LookupDto
{
    public int Id { get; init; }

    public string? Title { get; init; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            // Add your entity mappings here
            // Example: CreateMap<YourEntity, LookupDto>();
        }
    }
}
