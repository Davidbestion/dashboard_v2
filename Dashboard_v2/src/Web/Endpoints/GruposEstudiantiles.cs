using Dashboard_v2.Application.GruposEstudiantiles;
using Dashboard_v2.Web.Infrastructure;
using RolesEnum = Dashboard_v2.Domain.Enums.Roles;

namespace Dashboard_v2.Web.Endpoints;

/// <summary>
/// CRUD de Grupos Científicos Estudiantiles bajo /api/GruposEstudiantiles.
/// Similar a GruposDeInvestigacion pero sin relación con usuarios.
/// GET all → Superuser o Jefe_de_Grupo_de_investigacion.
/// POST/PUT/DELETE → Superuser o Jefe_de_Grupo_de_investigacion.
/// </summary>
public class GruposEstudiantiles : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet("", GetGruposEstudiantiles)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser)))
            .WithName("GetGruposEstudiantiles")
            .Produces<List<GrupoEstudiantilDto>>(200);

        groupBuilder.MapGet("area", GetAreaGruposEstudiantiles)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Vicedecano_de_investigacion)))
            .WithName("GetAreaGruposEstudiantiles")
            .Produces<List<GrupoEstudiantilDto>>(200);

        groupBuilder.MapPost("", CreateGrupoEstudiantil)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser)))
            .WithName("CreateGrupoEstudiantil")
            .Produces(201)
            .ProducesProblem(400);

        groupBuilder.MapPut("{id}", UpdateGrupoEstudiantil)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser)))
            .WithName("UpdateGrupoEstudiantil")
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(404);

        groupBuilder.MapDelete("{id}", DeleteGrupoEstudiantil)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser)))
            .WithName("DeleteGrupoEstudiantil")
            .Produces(200)
            .ProducesProblem(404);
    }

    private async Task<IResult> GetGruposEstudiantiles(IGrupoEstudiantilService svc)
    {
        var list = await svc.GetAllAsync();
        return Results.Ok(list);
    }

    private async Task<IResult> GetAreaGruposEstudiantiles(IGrupoEstudiantilService svc)
    {
        var list = await svc.GetAreaAsync();
        return Results.Ok(list);
    }

    private async Task<IResult> CreateGrupoEstudiantil(IGrupoEstudiantilService svc, CreateGrupoEstudiantilRequest body)
    {
        var (result, id) = await svc.CreateAsync(body);

        if (!result.Succeeded)
            return Results.BadRequest(new { errors = result.Errors });

        return Results.Created($"/api/GruposEstudiantiles/{id}", new { id });
    }

    private async Task<IResult> UpdateGrupoEstudiantil(IGrupoEstudiantilService svc, string id, UpdateGrupoEstudiantilRequest body)
    {
        var result = await svc.UpdateAsync(id, body);

        if (!result.Succeeded)
        {
            if (result.Errors.Contains("Grupo estudiantil no encontrado."))
                return Results.NotFound(new { errors = result.Errors });
            return Results.BadRequest(new { errors = result.Errors });
        }

        return Results.Ok(new { message = "Grupo estudiantil actualizado." });
    }

    private async Task<IResult> DeleteGrupoEstudiantil(IGrupoEstudiantilService svc, string id)
    {
        var result = await svc.DeleteAsync(id);

        if (!result.Succeeded)
        {
            if (result.Errors.Contains("Grupo estudiantil no encontrado."))
                return Results.NotFound(new { errors = result.Errors });
            return Results.BadRequest(new { errors = result.Errors });
        }

        return Results.Ok(new { message = "Grupo estudiantil eliminado." });
    }
}

// Request DTOs for create/update are defined in Application/GruposEstudiantiles/GrupoEstudiantilRequests.cs
