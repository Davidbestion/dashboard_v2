using Dashboard_v2.Application.Publications;
using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Publications.Commands.CreatePublication;
using Dashboard_v2.Application.Publications.Commands.DeletePublication;
using Dashboard_v2.Domain.Enums;
using RolesEnum = Dashboard_v2.Domain.Enums.Roles;
using Dashboard_v2.Application.Publications.Commands.UpdatePublication;
using Dashboard_v2.Application.Publications.Queries.GetMyPublications;
using Dashboard_v2.Application.Publications.Queries.GetPublicationById;
using Dashboard_v2.Application.Publications.Queries.GetPublicationTypes;
using Microsoft.EntityFrameworkCore;

namespace Dashboard_v2.Web.Endpoints;

/// <summary>
/// Endpoints de gestión de publicaciones académicas bajo /api/Publications.
/// Todos requieren el rol "Profesor".
/// </summary>
public class Publications : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        // GET /api/Publications/types — lista los tipos disponibles (para el selector)
        groupBuilder.MapGet("types", GetPublicationTypes)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Proyecto)))
            .WithName("GetPublicationTypes")
            .Produces<List<PublicationTypeDto>>(200);

        // GET /api/Publications/todas — todas las publicaciones (lectura, Superuser + Jefe_de_Proyecto)
        groupBuilder.MapGet("todas", GetTodasLasPublicaciones)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Proyecto)))
            .WithName("GetTodasLasPublicaciones")
            .Produces<List<PublicacionResumenDto>>(200);

        // GET /api/Publications — publicaciones del usuario autenticado
        groupBuilder.MapGet("", GetMyPublications)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor)))
            .WithName("GetMyPublications")
            .Produces<List<PublicationDto>>(200);

        // GET /api/Publications/{id}
        groupBuilder.MapGet("{id}", GetPublicationById)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor)))
            .WithName("GetPublicationById")
            .Produces<PublicationDto>(200)
            .ProducesProblem(404);

        // POST /api/Publications
        groupBuilder.MapPost("", CreatePublication)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Superuser), nameof(RolesEnum.Jefe_de_Proyecto)))
            .WithName("CreatePublication")
            .Produces(201)
            .ProducesProblem(400);

        // PUT /api/Publications/{id}
        groupBuilder.MapPut("{id}", UpdatePublication)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor)))
            .WithName("UpdatePublication")
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(404);

        // DELETE /api/Publications/{id}
        groupBuilder.MapDelete("{id}", DeletePublication)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor)))
            .WithName("DeletePublication")
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(404);
    }

    private async Task<IResult> GetPublicationTypes(ISender sender)
    {
        var types = await sender.Send(new GetPublicationTypesQuery());
        return Results.Ok(types);
    }

    private static async Task<IResult> GetTodasLasPublicaciones(IApplicationDbContext context)
    {
        var pubs = await context.Publications
            .AsNoTracking()
            .OrderBy(p => p.Title)
            .Select(p => new PublicacionResumenDto(
                p.Id,
                p.Title,
                p.UrlDoi,
                (int)p.PublicationType,
                p.ProyectoId,
                p.Proyecto != null ? p.Proyecto.Titulo : null))
            .ToListAsync();
        return Results.Ok(pubs);
    }

    private async Task<IResult> GetMyPublications(ISender sender)
    {
        var publications = await sender.Send(new GetMyPublicationsQuery());
        return Results.Ok(publications);
    }

    private async Task<IResult> GetPublicationById(ISender sender, string id)
    {
        var publication = await sender.Send(new GetPublicationByIdQuery(id));
        return publication is null ? Results.NotFound() : Results.Ok(publication);
    }

    private async Task<IResult> CreatePublication(ISender sender, CreatePublicationCommand command)
    {
        var (result, id) = await sender.Send(command);

        if (!result.Succeeded)
            return Results.BadRequest(new { errors = result.Errors });

        return Results.Created($"/api/Publications/{id}", new { id });
    }

    private async Task<IResult> UpdatePublication(ISender sender, string id, UpdatePublicationBody body)
    {
        var result = await sender.Send(new UpdatePublicationCommand
        {
            Id = id,
            Title = body.Title,
            PublicationData = body.PublicationData,
            PublicationType = (PublicationType)body.PublicationType,
            UrlDoi = body.UrlDoi,
            AdditionalAuthorIds = body.AdditionalAuthorIds ?? [],
            AdditionalAuthorNames = body.AdditionalAuthorNames ?? [],
            AdditionalUserIds = body.AdditionalUserIds ?? [],
            Index = body.Index,
            DataBase = body.DataBase,
            Group = body.Group,
            Cuartil = body.Cuartil,
            ProyectoId = body.ProyectoId,
        });

        if (!result.Succeeded)
            return Results.BadRequest(new { errors = result.Errors });

        return Results.Ok(new { message = "Publicación actualizada." });
    }

    private async Task<IResult> DeletePublication(ISender sender, string id)
    {
        var result = await sender.Send(new DeletePublicationCommand(id));

        if (!result.Succeeded)
            return Results.BadRequest(new { errors = result.Errors });

        return Results.Ok(new { message = "Publicación eliminada." });
    }
}

/// <summary>Cuerpo de la petición PUT para actualizar una publicación.</summary>
public record UpdatePublicationBody(
    string Title,
    string PublicationData,
    int PublicationType,
    string? UrlDoi,
    List<string>? AdditionalAuthorIds,
    List<string>? AdditionalAuthorNames,
    List<string>? AdditionalUserIds,
    // Especialización
    string? Index,
    string? DataBase,
    int? Group,
    string? Cuartil,
    string? ProyectoId);

/// <summary>DTO de lectura rápida de publicaciones para el rol Jefe_de_Proyecto.</summary>
public record PublicacionResumenDto(
    string Id,
    string Titulo,
    string? UrlDoi,
    int Tipo,
    string? ProyectoId,
    string? ProyectoTitulo);
