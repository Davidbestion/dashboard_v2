using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Common.Models;
using Dashboard_v2.Domain.Entities;
using RolesEnum = global::Dashboard_v2.Domain.Enums.Roles;

namespace Dashboard_v2.Application.Proyectos;

/// <summary>Métodos estáticos compartidos por todos los handlers de proyectos.</summary>
internal static class ProyectoHelper
{
    internal static void SetBase(Proyecto p, string titulo, string jefeId,
        int numMiembros, int cantUH, int cantEst, int cantEstCont, bool tributaFormacion, string clasificId)
    {
        p.Titulo = titulo.Trim();
        p.JefeId = jefeId;
        p.NumeroMiembros = numMiembros;
        p.CantidadMiembrosUH = cantUH;
        p.CantidadEstudiantes = cantEst;
        p.CantidadEstudiantesContratados = cantEstCont;
        p.TributaFormacionDoctoral = tributaFormacion;
        p.ClasificacionId = clasificId;
    }

    internal static void SetEjecucion(ProyectoEnEjecucion pe,
        DateOnly fechaInicio, DateOnly? fechaCierre,
        string codigoProyecto, bool tributaDesarrolloLocal)
    {
        pe.FechaInicio = fechaInicio;
        pe.FechaCierre = fechaCierre;
        pe.CodigoProyecto = codigoProyecto.Trim();
        pe.TributaDesarrolloLocal = tributaDesarrolloLocal;
    }

    /// <summary>
    /// Validates that a user exists and has the <c>Jefe_de_Proyecto</c> role.
    /// Returns a failure Result if validation fails, or null if valid.
    /// </summary>
    /// <param name="context">The application DB context.</param>
    /// <param name="jefeId">The ID of the user to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A failure Result if invalid; null if valid.</returns>
    internal static async Task<Result?> ValidateJefeAsync(
        IApplicationDbContext context, string jefeId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(jefeId))
            return Result.Failure(["El jefe del proyecto es obligatorio."]);

        var jefe = await context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == jefeId, cancellationToken);

        if (jefe is null)
            return Result.Failure(["El usuario indicado como jefe no existe."]);

        if (!jefe.UserRoles.Any(r => r.Role == RolesEnum.Jefe_de_Proyecto))
            return Result.Failure(["El usuario no tiene el rol de Jefe de Proyecto."]);

        return null;
    }

    /// <summary>
    /// Returns the current user's ID if they are a <c>Jefe_de_Proyecto</c> (restricts queries/mutations
    /// to their own projects), or <c>null</c> if they are a Superuser (no restriction applies).
    /// </summary>
    internal static string? GetOwnerFilter(IUser currentUser)
    {
        if (currentUser.Roles?.Contains(nameof(RolesEnum.Jefe_de_Proyecto)) == true)
            return currentUser.Id;
        return null;
    }

    /// <summary>
    /// Resolves the effective JefeId for a create/update operation.
    /// <para>
    /// If the caller is a <c>Jefe_de_Proyecto</c>, their own ID is always used (they cannot
    /// create or reassign projects to other jefes). If the caller is a Superuser, the provided
    /// <paramref name="requestedJefeId"/> is used as-is.
    /// </para>
    /// </summary>
    internal static string ResolveJefeId(string requestedJefeId, IUser currentUser)
    {
        if (currentUser.Roles?.Contains(nameof(RolesEnum.Jefe_de_Proyecto)) == true)
            return currentUser.Id ?? requestedJefeId;
        return requestedJefeId;
    }
}
