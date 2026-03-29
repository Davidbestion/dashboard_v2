using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Common.Models;
using Dashboard_v2.Domain.Entities;

namespace Dashboard_v2.Application.Publications.Commands.CreatePublicationType;

/// <summary>
/// Crea un nuevo tipo de publicación.<br/>
/// El nombre debe ser único; si ya existe, retorna error.
/// </summary>
public record CreatePublicationTypeCommand : IRequest<(Result Result, PublicationTypeDto? Type)>
{
    public string Name { get; init; } = default!;
}

public class CreatePublicationTypeCommandHandler
    : IRequestHandler<CreatePublicationTypeCommand, (Result Result, PublicationTypeDto? Type)>
{
    private readonly IApplicationDbContext _context;

    public CreatePublicationTypeCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(Result Result, PublicationTypeDto? Type)> Handle(
        CreatePublicationTypeCommand request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (string.IsNullOrEmpty(name))
            return (Result.Failure(["El nombre del tipo no puede estar vacío."]), null);

        var exists = await _context.PublicationTypes
            .AnyAsync(pt => pt.Name == name, cancellationToken);

        if (exists)
            return (Result.Failure([$"Ya existe un tipo de publicación con el nombre \"{name}\"."]), null);

        var type = new PublicationType { Name = name };
        _context.PublicationTypes.Add(type);
        await _context.SaveChangesAsync(cancellationToken);

        return (Result.Success(), new PublicationTypeDto { Id = type.Id, Name = type.Name });
    }
}
