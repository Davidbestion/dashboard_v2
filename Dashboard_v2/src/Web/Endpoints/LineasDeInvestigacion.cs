using Dashboard_v2.Application.LineasDeInvestigacion;
using Dashboard_v2.Web.Infrastructure;
using RolesEnum = Dashboard_v2.Domain.Enums.Roles;

namespace Dashboard_v2.Web.Endpoints;

/// <summary>
/// CRUD de Líneas de Investigación bajo /api/LineasDeInvestigacion. Solo Superuser.
/// El campo <c>areaDelConocimientoId</c> vincula la línea con un Área del Conocimiento.
/// </summary>
public class LineasDeInvestigacion : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet("", GetLineasDeInvestigacion)
            .RequireAuthorization()
            .WithName("GetLineasDeInvestigacion")
            .Produces<List<LineaDeInvestigacionDto>>(200);

        groupBuilder.MapPost("", CreateLineaDeInvestigacion)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser)))
            .WithName("CreateLineaDeInvestigacion")
            .Produces(201)
            .ProducesProblem(400);

        groupBuilder.MapPut("{id}", UpdateLineaDeInvestigacion)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser)))
            .WithName("UpdateLineaDeInvestigacion")
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(404);

        groupBuilder.MapDelete("{id}", DeleteLineaDeInvestigacion)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser)))
            .WithName("DeleteLineaDeInvestigacion")
            .Produces(200)
            .ProducesProblem(404);
    }

    private async Task<IResult> GetLineasDeInvestigacion(ILineaDeInvestigacionService svc)
    {
        var list = await svc.GetAllAsync();
        return Results.Ok(list);
    }

    private async Task<IResult> CreateLineaDeInvestigacion(ILineaDeInvestigacionService svc, CreateLineaDeInvestigacionRequest body)
    {
        var (result, id) = await svc.CreateAsync(body);

        if (!result.Succeeded)
            return Results.BadRequest(new { errors = result.Errors });

        return Results.Created($"/api/LineasDeInvestigacion/{id}", new { id });
    }

    private async Task<IResult> UpdateLineaDeInvestigacion(ILineaDeInvestigacionService svc, string id, UpdateLineaDeInvestigacionRequest body)
    {
        var result = await svc.UpdateAsync(id, body);

        if (!result.Succeeded)
            return result.Errors.Contains("Línea de investigación no encontrada.")
                ? Results.NotFound(new { errors = result.Errors })
                : Results.BadRequest(new { errors = result.Errors });

        return Results.Ok(new { message = "Línea de investigación actualizada." });
    }

    private async Task<IResult> DeleteLineaDeInvestigacion(ILineaDeInvestigacionService svc, string id)
    {
        var result = await svc.DeleteAsync(id);

        if (!result.Succeeded)
            return Results.NotFound(new { errors = result.Errors });

        return Results.Ok(new { message = "Línea de investigación eliminada." });
    }
}

// Request DTOs for create/update are defined in Application/LineasDeInvestigacion/LineaRequests.cs
