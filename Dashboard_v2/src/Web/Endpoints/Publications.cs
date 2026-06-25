using Dashboard_v2.Application.Publications;
using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Domain.Enums;
using RolesEnum = Dashboard_v2.Domain.Enums.Roles;
using Microsoft.EntityFrameworkCore;

namespace Dashboard_v2.Web.Endpoints;

/// <summary>
/// API endpoints for managing academic publications: CRUD, CrossRef/OpenAire integration, and publication database resolution.
/// </summary>
public class Publications : EndpointGroupBase
{
    /// <summary>Registers the Publications route group with all publication endpoints.</summary>
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        // GET /api/Publications/types — lista los tipos disponibles (para el selector)
        groupBuilder.MapGet("types", GetPublicationTypes)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Superuser)))
            .WithName("GetPublicationTypes")
            .Produces<List<PublicationTypeDto>>(200);

        // GET /api/Publications/todas — todas las publicaciones (solo Superuser)
        groupBuilder.MapGet("todas", GetTodasLasPublicaciones)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Superuser)))
            .WithName("GetTodasLasPublicaciones")
            .Produces<List<PublicationDto>>(200);

        // GET /api/Publications/proyecto — publicaciones vinculadas a los proyectos que lidera el jefe
        groupBuilder.MapGet("proyecto", GetProyectoPublications)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Jefe_de_Proyecto)))
            .WithName("GetProyectoPublications")
            .Produces<List<PublicationDto>>(200);

        // GET /api/Publications/area — publicaciones del área del usuario (Vicedecano de investigación)
        groupBuilder.MapGet("area", GetAreaPublications)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Vicedecano_de_investigacion)))
            .WithName("GetAreaPublications")
            .Produces<List<PublicationDto>>(200);

        // GET /api/Publications/redes
        // Jefe_de_Redes → todas las publicaciones con RedId.
        // Profesor       → solo las de las redes que coordina.
        groupBuilder.MapGet("redes", GetMyRedPublications)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Jefe_de_Redes), nameof(RolesEnum.Profesor)))
            .WithName("GetMyRedPublications")
            .Produces<List<PublicationDto>>(200);

        // GET /api/Publications — publicaciones del usuario autenticado
        groupBuilder.MapGet("", GetMyPublications)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Superuser)))
            .WithName("GetMyPublications")
            .Produces<List<PublicationDto>>(200);

        // GET /api/Publications/{id}
        groupBuilder.MapGet("{id}", GetPublicationById)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Superuser)))
            .WithName("GetPublicationById")
            .Produces<PublicationDto>(200)
            .ProducesProblem(404);

        // GET /api/Publications/public/{id} — obtener detalle público de una publicación (sin exigir ser autor)
        groupBuilder.MapGet("public/{id}", GetPublicationPublicById)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Superuser)))
            .WithName("GetPublicationPublicById")
            .Produces<PublicationDto>(200)
            .ProducesProblem(404);

        // POST /api/Publications
        groupBuilder.MapPost("", CreatePublication)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Superuser)))
            .WithName("CreatePublication")
            .Produces(201)
            .ProducesProblem(400);

        // GET /api/Publications/duplicates?title=...&doi=...&url=...
        groupBuilder.MapGet("duplicates", FindDuplicates)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Superuser)))
            .WithName("FindPublicationDuplicates")
            .Produces<List<PublicationDuplicateDto>>(200);

        // GET /api/Publications/crossref?doi=&title=
        groupBuilder.MapGet("crossref", GetCrossRefCandidates)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Superuser)))
            .WithName("GetCrossRefCandidates")
            .Produces<List<PublicationCrossRefDto>>(200);

        // GET /api/Publications/openaire?doi=&title=
        // Searches OpenAIRE — covers SciELO, PubMed, institutional repos and more.
        groupBuilder.MapGet("openaire", GetOpenAireCandidates)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Superuser)))
            .WithName("GetOpenAireCandidates")
            .Produces<List<PublicationCrossRefDto>>(200);

        // GET /api/Publications/resolve-database?doi=&title=
        // Best-effort: fetch CrossRef metadata for the DOI/title and resolve
        // the journal's database/group using configured providers.
        groupBuilder.MapGet("resolve-database", ResolveDatabaseFromCrossRef)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Superuser)))
            .WithName("ResolvePublicationDatabaseFromCrossRef")
            .Produces<Dashboard_v2.Application.Publications.PublicationDatabaseMatchDto>(200)
            .ProducesProblem(404);

        // POST /api/Publications/{id}/coauthors -> assign current user as coauthor (idempotent)
        groupBuilder.MapPost("{id}/coauthors", AddCurrentUserAsCoauthor)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Superuser)))
            .WithName("AddCurrentUserAsCoauthor")
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(404);

        // PUT /api/Publications/{id}
        groupBuilder.MapPut("{id}", UpdatePublication)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Superuser)))
            .WithName("UpdatePublication")
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(404);

        // DELETE /api/Publications/{id}
        groupBuilder.MapDelete("{id}", DeletePublication)
            .RequireAuthorization(p => p.RequireRole(nameof(RolesEnum.Profesor), nameof(RolesEnum.Superuser)))
            .WithName("DeletePublication")
            .Produces(200)
            .ProducesProblem(400)
            .ProducesProblem(404);
    }

    private async Task<IResult> GetPublicationTypes(IPublicationService service)
    {
        var types = await service.GetPublicationTypesAsync();
        return Results.Ok(types);
    }

    private async Task<IResult> GetTodasLasPublicaciones(IPublicationService service)
    {
        var pubs = await service.GetAllPublicationsAsync();
        return Results.Ok(pubs);
    }

    private async Task<IResult> GetAreaPublications(IPublicationService service)
    {
        var pubs = await service.GetAreaPublicationsAsync();
        return Results.Ok(pubs);
    }

    private async Task<IResult> GetMyRedPublications(IPublicationService service)
    {
        var pubs = await service.GetMyRedPublicationsAsync();
        return Results.Ok(pubs);
    }

    private async Task<IResult> GetProyectoPublications(IPublicationService service)
    {
        var pubs = await service.GetProyectoPublicationsAsync();
        return Results.Ok(pubs);
    }

    private async Task<IResult> GetMyPublications(IPublicationService service)
    {
        var publications = await service.GetMyPublicationsAsync();
        return Results.Ok(publications);
    }

    private async Task<IResult> GetPublicationById(IPublicationService service, string id)
    {
        var publication = await service.GetByIdAsync(id);
        return publication is null ? Results.NotFound() : Results.Ok(publication);
    }

    private async Task<IResult> GetPublicationPublicById(IPublicationService service, string id)
    {
        var publication = await service.GetPublicByIdAsync(id);
        return publication is null ? Results.NotFound() : Results.Ok(publication);
    }

    private async Task<IResult> CreatePublication(IPublicationService service, CreatePublicationRequest command)
    {
        var (result, id) = await service.CreateAsync(command);

        if (!result.Succeeded)
            return Results.BadRequest(new { errors = result.Errors });

        return Results.Created($"/api/Publications/{id}", new { id });
    }

    private async Task<IResult> FindDuplicates(IPublicationService service, string? title, string? doi, string? url, string? excludeId)
    {
        var candidates = await service.FindDuplicatesAsync(title, doi, url, excludeId);
        return Results.Ok(candidates);
    }

    private async Task<IResult> GetCrossRefCandidates(IPublicationService service, string? doi, string? title)
    {
        var items = await service.SearchCrossRefCandidatesAsync(doi, title);
        return Results.Ok(items);
    }

    private async Task<IResult> GetOpenAireCandidates(IPublicationService service, string? doi, string? title)
    {
        var items = await service.SearchOpenAireCandidatesAsync(doi, title);
        return Results.Ok(items);
    }

    /// <summary>
    /// Fetches CrossRef metadata for the given DOI and resolves the best bibliographic database match.
    /// </summary>
    private async Task<IResult> ResolveDatabaseFromCrossRef(IPublicationService service, string? doi, string? title, string? issns, string? publishedDate)
        => Results.Ok(await service.ResolveDatabaseFromCrossRefAsync(doi, title, issns, publishedDate));

    private async Task<IResult> AddCurrentUserAsCoauthor(IPublicationService service, string id)
    {
        var result = await service.AddCurrentUserAsCoauthorAsync(id);
        if (!result.Succeeded) return Results.BadRequest(new { errors = result.Errors });
        return Results.Ok(new { message = "Se ha añadido al usuario como coautor (si no lo era)." });
    }

    private async Task<IResult> UpdatePublication(IPublicationService service, string id, UpdatePublicationBody body)
    {
        var req = new UpdatePublicationRequest
        {
            Id = id,
            Title = body.Title,
            PublicationData = body.PublicationData,
            PublicationType = (PublicationType)body.PublicationType,
            UrlDoi = body.UrlDoi,
            PublishedDate = body.PublishedDate,
            AdditionalAuthorIds = body.AdditionalAuthorIds ?? [],
            AdditionalAuthorNames = body.AdditionalAuthorNames ?? [],
            AdditionalUserIds = body.AdditionalUserIds ?? [],
            Index = body.Index,
            DataBase = body.DataBase,
            Group = body.Group,
            Cuartil = body.Cuartil,
            ProyectoId = body.ProyectoId,
            RedId = body.RedId,
            EvidenceFileId = body.EvidenceFileId,
        };

        var result = await service.UpdateAsync(req);

        if (!result.Succeeded)
            return Results.BadRequest(new { errors = result.Errors });

        return Results.Ok(new { message = "Publicación actualizada." });
    }

    private async Task<IResult> DeletePublication(IPublicationService service, string id)
    {
        var result = await service.DeleteAsync(id);

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
    string PublishedDate,
    List<string>? AdditionalAuthorIds,
    List<string>? AdditionalAuthorNames,
    List<string>? AdditionalUserIds,
    // Especialización
    int? Index,
    string? DataBase,
    int? Group,
    string? Cuartil,
    string? ProyectoId,
    string? RedId,
    int? EvidenceFileId);
