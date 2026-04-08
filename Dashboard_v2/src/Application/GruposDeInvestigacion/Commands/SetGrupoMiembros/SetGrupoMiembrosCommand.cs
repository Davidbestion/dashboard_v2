using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Common.Models;

namespace Dashboard_v2.Application.GruposDeInvestigacion.Commands.SetGrupoMiembros;

/// <summary>
/// Reemplaza la lista de miembros de un Grupo de Investigación.
/// Solo puede ejecutarlo un Superuser o alguien que ya sea miembro del grupo.
/// </summary>
public record SetGrupoMiembrosCommand : IRequest<Result>
{
    public string GrupoId { get; init; } = default!;
    public IList<string> UsuariosIds { get; init; } = [];
}

public class SetGrupoMiembrosCommandHandler : IRequestHandler<SetGrupoMiembrosCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _currentUser;

    public SetGrupoMiembrosCommandHandler(IApplicationDbContext context, IUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(SetGrupoMiembrosCommand request, CancellationToken cancellationToken)
    {
        var grupo = await _context.GruposDeInvestigacion
            .Include(g => g.Usuarios)
            .FirstOrDefaultAsync(g => g.Id == request.GrupoId, cancellationToken);

        if (grupo is null)
            return Result.Failure(["Grupo de investigación no encontrado."]);

        var isSuperuser = _currentUser.Roles?.Contains("Superuser") == true;
        var isJefe = _currentUser.Roles?.Contains("Jefe_de_Grupo_de_investigacion") == true;
        if (!isSuperuser && !isJefe)
            return Result.Failure(["No tienes permisos para gestionar los miembros de este grupo."]);

        var newUsuarios = await _context.Users
            .Where(u => request.UsuariosIds.Contains(u.Id))
            .ToListAsync(cancellationToken);

        grupo.Usuarios.Clear();
        foreach (var usuario in newUsuarios)
            grupo.Usuarios.Add(usuario);

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
