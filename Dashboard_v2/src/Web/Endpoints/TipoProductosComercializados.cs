using Dashboard_v2.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Dashboard_v2.Web.Endpoints;

/// <summary>
/// API endpoints for commercialized product type management.
/// </summary>
public class TipoProductosComercializados : EndpointGroupBase
{
    /// <summary>Registers the TipoProductosComercializados route group with the product type list endpoint.</summary>
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet("", GetAll)
            .RequireAuthorization()
            .WithName("GetTipoProductosComercializados")
            .Produces<List<TipoProductoDto>>(200);
    }

    private async Task<IResult> GetAll(IApplicationDbContext db)
    {
        var list = await db.TipoProductosComercializados
            .Select(t => new TipoProductoDto(t.Id, t.Nombre))
            .ToListAsync();

        return Results.Ok(list);
    }
}

public record TipoProductoDto(string Id, string Nombre);
