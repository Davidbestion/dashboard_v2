using Dashboard_v2.Application.Users;
using Dashboard_v2.Web.Infrastructure;
using RolesEnum = Dashboard_v2.Domain.Enums.Roles;

namespace Dashboard_v2.Web.Endpoints;

/// <summary>
/// API endpoints for user account and role management.
/// </summary>
public class Users : EndpointGroupBase
{
    /// <summary>Registers the Users route group with user listing, creation, role assignment, and activation endpoints.</summary>
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet("", GetUsers)
            .RequireAuthorization(policy => policy.RequireRole(
                nameof(RolesEnum.Superuser),
                nameof(RolesEnum.Jefe_de_Grupo_de_investigacion),
                nameof(RolesEnum.Jefe_de_Redes),
                nameof(RolesEnum.Profesor),
                nameof(RolesEnum.Vicedecano_de_investigacion),
                nameof(RolesEnum.Jefe_de_Proyecto)))
            .WithName("GetUsers")
            .Produces<List<UserWithRolesDto>>(200);

        groupBuilder.MapPost("", CreateUser)
            .RequireAuthorization(policy => policy.RequireRole(nameof(RolesEnum.Superuser)))
            .WithName("CreateUser")
            .Produces(201)
            .ProducesProblem(400);

        groupBuilder.MapPost("{userId}/roles", AssignRole)
            .RequireAuthorization(policy => policy.RequireRole(nameof(RolesEnum.Superuser)))
            .WithName("AssignRole")
            .Produces(200)
            .ProducesProblem(400);

        groupBuilder.MapDelete("{userId}/roles/{roleName}", RemoveRole)
            .RequireAuthorization(policy => policy.RequireRole(nameof(RolesEnum.Superuser)))
            .WithName("RemoveRole")
            .Produces(200)
            .ProducesProblem(400);

        groupBuilder.MapPut("{userId}/active", SetActive)
            .RequireAuthorization(policy => policy.RequireRole(nameof(RolesEnum.Superuser)))
            .WithName("SetUserActive")
            .Produces(200)
            .ProducesProblem(400);

        groupBuilder.MapGet("jefes-de-proyecto", GetJefesDeProyecto)
            .RequireAuthorization(policy => policy.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Proyecto)))
            .WithName("GetJefesDeProyecto")
            .Produces<List<JefeDeProyectoDto>>(200);
    }

    /// <summary>GET /api/Users/jefes-de-proyecto — Retorna usuarios activos con rol Jefe_de_Proyecto.</summary>
    private async Task<IResult> GetJefesDeProyecto(IUserService service)
    {
        var jefes = await service.GetJefesDeProyectoAsync();
        return Results.Ok(jefes);
    }

    /// <summary>GET /api/Users — Retorna todos los usuarios con sus roles. Solo Superuser.</summary>
    private async Task<IResult> GetUsers(IUserService service)
    {
        var users = await service.GetAllAsync();
        return Results.Ok(users);
    }

    /// <summary>
    /// POST /api/Users/{userId}/roles — Asigna un rol al usuario indicado. Solo Superuser.<br/>
    /// El cuerpo JSON debe contener <c>{ "roleName": "Profesor" }</c> (nombre exacto del enum).
    /// </summary>
    private async Task<IResult> AssignRole(IUserService service, string userId, AssignRoleRequest body)
    {
        var result = await service.AssignRoleAsync(userId, body.RoleName);
        return result.Succeeded
            ? Results.Ok(new { message = "Rol asignado correctamente." })
            : Results.BadRequest(new { errors = result.Errors });
    }

    private async Task<IResult> RemoveRole(IUserService service, string userId, string roleName)
    {
        var result = await service.RemoveRoleAsync(userId, roleName);
        return result.Succeeded
            ? Results.Ok(new { message = "Rol eliminado correctamente." })
            : Results.BadRequest(new { errors = result.Errors });
    }

    private async Task<IResult> SetActive(IUserService service, string userId, SetActiveRequest body)
    {
        var result = await service.SetActiveAsync(userId, body.Active);
        return result.Succeeded
            ? Results.Ok(new { message = body.Active ? "Usuario activado." : "Usuario desactivado." })
            : Results.BadRequest(new { errors = result.Errors });
    }

    private async Task<IResult> CreateUser(IUserService service, CreateUserRequest body)
    {
        var (result, userId) = await service.CreateUserAsync(body);
        return result.Succeeded
            ? Results.Created($"/api/Users/{userId}", new { id = userId })
            : Results.BadRequest(new { errors = result.Errors });
    }
}

public record AssignRoleRequest(string RoleName);
public record SetActiveRequest(bool Active);
