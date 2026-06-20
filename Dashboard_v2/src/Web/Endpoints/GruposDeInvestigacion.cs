using Dashboard_v2.Application.GruposDeInvestigacion;
using Dashboard_v2.Web.Infrastructure;
using RolesEnum = Dashboard_v2.Domain.Enums.Roles;

namespace Dashboard_v2.Web.Endpoints;

/// <summary>
/// CRUD de Grupos de Investigación bajo /api/GruposDeInvestigacion.
/// GET all → solo Superuser.
/// GET /mine → cualquier usuario autenticado (devuelve sus propios grupos).
/// POST/PUT/DELETE → Superuser o Jefe_de_Grupo_de_investigacion (con validación de pertenencia).
/// </summary>
public class GruposDeInvestigacion : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet("", GetGruposDeInvestigacion)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Grupo_de_investigacion)))
            .WithName("GetGruposDeInvestigacion")
            .Produces<List<GrupoDeInvestigacionDto>>(200);

        groupBuilder.MapGet("mine", GetMisGruposDeInvestigacion)
            .RequireAuthorization()
            .WithName("GetMisGruposDeInvestigacion")
            .Produces<List<GrupoDeInvestigacionDto>>(200);

        groupBuilder.MapGet("area", GetAreaGruposDeInvestigacion)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Vicedecano_de_investigacion)))
            .WithName("GetAreaGruposDeInvestigacion")
            .Produces<List<GrupoDeInvestigacionDto>>(200);

        groupBuilder.MapPost("", CreateGrupoDeInvestigacion)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Grupo_de_investigacion)))
            .WithName("CreateGrupoDeInvestigacion")
            .Produces(201)
            .ProducesProblem(400);

        groupBuilder.MapPut("{id}", UpdateGrupoDeInvestigacion)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Grupo_de_investigacion)))
            .WithName("UpdateGrupoDeInvestigacion")
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(404);

        groupBuilder.MapDelete("{id}", DeleteGrupoDeInvestigacion)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Grupo_de_investigacion)))
            .WithName("DeleteGrupoDeInvestigacion")
            .Produces(200)
            .ProducesProblem(404);

        groupBuilder.MapPut("{id}/miembros", SetGrupoMiembros)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Grupo_de_investigacion)))
            .WithName("SetGrupoMiembros")
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(404);
    }

    private async Task<IResult> GetGruposDeInvestigacion(IGrupoDeInvestigacionService svc)
    {
        var list = await svc.GetAllAsync();
        return Results.Ok(list);
    }

    private async Task<IResult> GetMisGruposDeInvestigacion(IGrupoDeInvestigacionService svc)
    {
        var list = await svc.GetMineAsync();
        return Results.Ok(list);
    }

    private async Task<IResult> GetAreaGruposDeInvestigacion(IGrupoDeInvestigacionService svc)
    {
        var list = await svc.GetAreaAsync();
        return Results.Ok(list);
    }

    private async Task<IResult> CreateGrupoDeInvestigacion(IGrupoDeInvestigacionService svc, CreateGrupoDeInvestigacionRequest body)
    {
        var (result, id) = await svc.CreateAsync(body);

        if (!result.Succeeded)
            return Results.BadRequest(new { errors = result.Errors });

        return Results.Created($"/api/GruposDeInvestigacion/{id}", new { id });
    }

    private async Task<IResult> UpdateGrupoDeInvestigacion(IGrupoDeInvestigacionService svc, string id, UpdateGrupoDeInvestigacionRequest body)
    {
        var result = await svc.UpdateAsync(id, body);

        if (!result.Succeeded)
        {
            if (result.Errors.Contains("Grupo de investigación no encontrado."))
                return Results.NotFound(new { errors = result.Errors });
            return Results.BadRequest(new { errors = result.Errors });
        }

        return Results.Ok(new { message = "Grupo de investigación actualizado." });
    }

    private async Task<IResult> DeleteGrupoDeInvestigacion(IGrupoDeInvestigacionService svc, string id)
    {
        var result = await svc.DeleteAsync(id);

        if (!result.Succeeded)
        {
            if (result.Errors.Contains("Grupo de investigación no encontrado."))
                return Results.NotFound(new { errors = result.Errors });
            return Results.BadRequest(new { errors = result.Errors });
        }

        return Results.Ok(new { message = "Grupo de investigación eliminado." });
    }

    private async Task<IResult> SetGrupoMiembros(IGrupoDeInvestigacionService svc, string id, SetGrupoMiembrosRequest body)
    {
        var result = await svc.SetMiembrosAsync(id, body);

        if (!result.Succeeded)
        {
            if (result.Errors.Contains("Grupo de investigación no encontrado."))
                return Results.NotFound(new { errors = result.Errors });
            return Results.BadRequest(new { errors = result.Errors });
        }

        return Results.Ok(new { message = "Miembros actualizados." });
    }
}

// Request DTOs for create/update/set miembros are defined in Application/GruposDeInvestigacion/GrupoRequests.cs

