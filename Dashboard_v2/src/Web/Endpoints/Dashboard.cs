using Dashboard_v2.Application.Dashboard;
using RolesEnum = Dashboard_v2.Domain.Enums.Roles;

namespace Dashboard_v2.Web.Endpoints;

/// <summary>
/// API endpoint for the Vicedecano institutional research dashboard.
/// </summary>
public class Dashboard : EndpointGroupBase
{
    /// <summary>Registers the Dashboard route group with the Vicedecano dashboard endpoint.</summary>
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet("vicedecano", GetVicedecanoDashboard)
            .RequireAuthorization(p => p.RequireRole(
                nameof(RolesEnum.Vicedecano_de_investigacion),
                nameof(RolesEnum.Superuser)))
            .WithName("GetVicedecanoDashboard")
            .Produces<VicedecanoDashboardDto>(200);
    }

    private static async Task<IResult> GetVicedecanoDashboard(IDashboardService service, CancellationToken ct)
        => Results.Ok(await service.GetVicedecanoDashboardAsync(ct));
}
