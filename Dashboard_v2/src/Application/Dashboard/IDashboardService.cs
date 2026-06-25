namespace Dashboard_v2.Application.Dashboard;

/// <summary>
/// Contract for aggregating institutional research statistics for dashboard views.
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Aggregates research statistics for the Vicedecano dashboard: publications by type/group/author,
    /// projects by type/state, events, awards, networks, patents, and more.
    /// Filters by the current user's academic area.
    /// </summary>
    Task<VicedecanoDashboardDto> GetVicedecanoDashboardAsync(CancellationToken ct = default);
}
