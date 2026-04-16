using Dashboard_v2.Application.Users.Commands.AssignRole;
using Dashboard_v2.Application.Users.Commands.RemoveRole;
using Dashboard_v2.Application.Users.Queries.GetJefesDeProyecto;
using Dashboard_v2.Application.Users.Queries.GetUsers;
using Dashboard_v2.Web.Infrastructure;

namespace Dashboard_v2.Web.Endpoints;

/// <summary>
/// Grupo de endpoints de administración de usuarios, mapeado bajo <c>/api/Users</c>.<br/>
/// Todos los endpoints requieren el rol <c>Superuser</c> — cualquier otro rol recibe 403.
/// </summary>
public class Users : EndpointGroupBase
{
    /// <summary>Registra GET (listar), POST /{id}/roles (asignar) y DELETE /{id}/roles/{rol} (quitar).</summary>
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet("", GetUsers)
            .RequireAuthorization(policy => policy.RequireRole("Superuser", "Jefe_de_Grupo_de_investigacion"))
            .WithName("GetUsers")
            .Produces<List<UserWithRolesDto>>(200);

        groupBuilder.MapPost("{userId}/roles", AssignRole)
            .RequireAuthorization(policy => policy.RequireRole("Superuser"))
            .WithName("AssignRole")
            .Produces(200)
            .ProducesProblem(400);

        groupBuilder.MapDelete("{userId}/roles/{roleName}", RemoveRole)
            .RequireAuthorization(policy => policy.RequireRole("Superuser"))
            .WithName("RemoveRole")
            .Produces(200)
            .ProducesProblem(400);

        groupBuilder.MapGet("jefes-de-proyecto", GetJefesDeProyecto)
            .RequireAuthorization(policy => policy.RequireRole("Superuser", "Jefe_de_Proyecto"))
            .WithName("GetJefesDeProyecto")
            .Produces<List<JefeDeProyectoDto>>(200);
    }

    /// <summary>GET /api/Users/jefes-de-proyecto — Retorna usuarios activos con rol Jefe_de_Proyecto.</summary>
    private async Task<IResult> GetJefesDeProyecto(ISender sender)
    {
        var jefes = await sender.Send(new GetJefesDeProyectoQuery());
        return Results.Ok(jefes);
    }

    /// <summary>GET /api/Users — Retorna todos los usuarios con sus roles. Solo Superuser.</summary>
    private async Task<IResult> GetUsers(ISender sender)
    {
        var users = await sender.Send(new GetUsersQuery());
        return Results.Ok(users);
    }

    /// <summary>
    /// POST /api/Users/{userId}/roles — Asigna un rol al usuario indicado. Solo Superuser.<br/>
    /// El cuerpo JSON debe contener <c>{ "roleName": "Profesor" }</c> (nombre exacto del enum).
    /// </summary>
    private async Task<IResult> AssignRole(ISender sender, string userId, AssignRoleRequest body)
    {
        var result = await sender.Send(new AssignRoleCommand { UserId = userId, RoleName = body.RoleName });
        return result.Succeeded
            ? Results.Ok(new { message = "Rol asignado correctamente." })
            : Results.BadRequest(new { errors = result.Errors });
    }

    /// <summary>
    /// DELETE /api/Users/{userId}/roles/{roleName} — Quita el rol especificado al usuario. Solo Superuser.
    /// </summary>
    private async Task<IResult> RemoveRole(ISender sender, string userId, string roleName)
    {
        var result = await sender.Send(new RemoveRoleCommand { UserId = userId, RoleName = roleName });
        return result.Succeeded
            ? Results.Ok(new { message = "Rol eliminado correctamente." })
            : Results.BadRequest(new { errors = result.Errors });
    }
}

/// <summary>Cuerpo de la petición para asignar un rol. <paramref name="RoleName"/> debe ser un valor válido del enum Roles.</summary>
public record AssignRoleRequest(string RoleName);
