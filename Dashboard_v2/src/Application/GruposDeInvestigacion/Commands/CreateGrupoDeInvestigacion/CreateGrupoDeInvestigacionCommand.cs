using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Common.Models;
using Dashboard_v2.Domain.Entities;

namespace Dashboard_v2.Application.GruposDeInvestigacion.Commands.CreateGrupoDeInvestigacion;

public record CreateGrupoDeInvestigacionCommand : IRequest<(Result Result, string? Id)>
{
    public string Nombre { get; init; } = default!;
    public string AreaId { get; init; } = default!;
    /// <summary>Ids de las Líneas de Investigación que estudia este grupo (relación N:N).</summary>
    public IList<string> LineasDeInvestigacionIds { get; init; } = [];
}

public class CreateGrupoDeInvestigacionCommandHandler
    : IRequestHandler<CreateGrupoDeInvestigacionCommand, (Result Result, string? Id)>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public CreateGrupoDeInvestigacionCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<(Result Result, string? Id)> Handle(
        CreateGrupoDeInvestigacionCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
            return (Result.Failure(["El nombre es obligatorio."]), null);

        if (!await _context.Areas.AnyAsync(a => a.Id == request.AreaId, cancellationToken))
            return (Result.Failure(["El área indicada no existe."]), null);

        var grupo = new GrupoDeInvestigacion
        {
            Nombre = request.Nombre.Trim(),
            AreaId = request.AreaId,
            CreadorId = _currentUser.Id
        };

        if (request.LineasDeInvestigacionIds.Count > 0)
        {
            var lineas = await _context.LineasDeInvestigacion
                .Where(l => request.LineasDeInvestigacionIds.Contains(l.Id))
                .ToListAsync(cancellationToken);
            grupo.LineasDeInvestigacion = lineas;
        }

        _context.GruposDeInvestigacion.Add(grupo);
        await _context.SaveChangesAsync(cancellationToken);

        return (Result.Success(), grupo.Id);
    }
}
