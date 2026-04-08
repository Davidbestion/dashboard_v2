using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Common.Models;

namespace Dashboard_v2.Application.GruposDeInvestigacion.Commands.DeleteGrupoDeInvestigacion;

public record DeleteGrupoDeInvestigacionCommand(string Id) : IRequest<Result>;

public class DeleteGrupoDeInvestigacionCommandHandler : IRequestHandler<DeleteGrupoDeInvestigacionCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public DeleteGrupoDeInvestigacionCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(DeleteGrupoDeInvestigacionCommand request, CancellationToken cancellationToken)
    {
        var grupo = await _context.GruposDeInvestigacion
            .FirstOrDefaultAsync(g => g.Id == request.Id, cancellationToken);

        if (grupo is null)
            return Result.Failure(["Grupo de investigación no encontrado."]);

        // Superuser o Jefe pueden eliminar cualquier grupo
        var isSuperuser = _currentUser.Roles?.Contains("Superuser") == true;
        var isJefe = _currentUser.Roles?.Contains("Jefe_de_Grupo_de_investigacion") == true;
        if (!isSuperuser && !isJefe)
            return Result.Failure(["No tienes permisos para eliminar este grupo."]);

        _context.GruposDeInvestigacion.Remove(grupo);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
