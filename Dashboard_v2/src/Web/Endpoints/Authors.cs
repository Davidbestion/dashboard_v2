using Dashboard_v2.Application.Authors;

namespace Dashboard_v2.Web.Endpoints;

/// <summary>
/// API endpoints for author search and co-author resolution.
/// </summary>
public class Authors : EndpointGroupBase
{
    /// <summary>Registers the Authors route group with search, co-author resolution, and linking endpoints.</summary>
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        // GET /api/Authors/search?q=...
        // Búsqueda de autores por nombre (autocompletado).
        groupBuilder.MapGet("search", SearchAuthors)
            .RequireAuthorization()
            .WithName("SearchAuthors")
            .Produces<List<AuthorSearchDto>>(200);

        // GET /api/Authors/search-coauthors?q=...
        // Búsqueda unificada: autores existentes + usuarios sin perfil de autor.
        groupBuilder.MapGet("search-coauthors", SearchCoauthors)
            .RequireAuthorization()
            .WithName("SearchCoauthors")
            .Produces<List<CoauthorSearchDto>>(200);

        // GET /api/Authors/potential-matches
        // Devuelve autores sin cuenta cuyo nombre coincide con el usuario autenticado.
        groupBuilder.MapGet("potential-matches", GetPotentialMatches)
            .RequireAuthorization()
            .WithName("GetPotentialAuthorMatches")
            .Produces<PotentialAuthorMatchesDto>(200);

        // POST /api/Authors/{id}/link-to-me
        // Vincula el autor indicado al usuario autenticado (previa confirmación del usuario).
        groupBuilder.MapPost("{id}/link-to-me", LinkToMe)
            .RequireAuthorization()
            .WithName("LinkAuthorToMe")
            .Produces(200)
            .ProducesProblem(400);

        // POST /api/Authors/resolve-external
        // Para cada nombre externo (CrossRef / OpenAIRE) devuelve la coincidencia
        // encontrada en el sistema (si existe) sin crear ningún autor.
        groupBuilder.MapPost("resolve-external", ResolveExternal)
            .RequireAuthorization()
            .WithName("ResolveExternalAuthors")
            .Produces<List<ExternalAuthorResolutionDto>>(200);
    }

    private async Task<IResult> SearchAuthors(IAuthorService service, string? q)
    {
        var results = await service.SearchAsync(q ?? string.Empty);
        return Results.Ok(results);
    }

    private async Task<IResult> SearchCoauthors(IAuthorService service, string? q)
    {
        var results = await service.SearchCoauthorsAsync(q ?? string.Empty);
        return Results.Ok(results);
    }

    private async Task<IResult> GetPotentialMatches(IAuthorService service)
    {
        var result = await service.GetPotentialAuthorMatchesAsync();
        return Results.Ok(result);
    }

    private async Task<IResult> LinkToMe(IAuthorService service, string id)
    {
        var result = await service.LinkToUserAsync(id);
        if (!result.Succeeded)
            return Results.BadRequest(new { errors = result.Errors });
        return Results.Ok(new { message = "Autor vinculado correctamente." });
    }

    private async Task<IResult> ResolveExternal(IAuthorService service, ResolveExternalAuthorsRequest body)
    {
        if (body?.Names == null || body.Names.Count == 0)
            return Results.Ok(new List<ExternalAuthorResolutionDto>());

        var result = await service.ResolveExternalAuthorsAsync(body.Names);
        return Results.Ok(result);
    }
}

/// <param name="Names">Lista de nombres externos en formato "Apellidos, Nombres".</param>
public record ResolveExternalAuthorsRequest(List<string> Names);
