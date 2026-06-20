namespace Dashboard_v2.Application.Dashboard;

public interface IDashboardService
{
    Task<VicedecanoDashboardDto> GetVicedecanoDashboardAsync(CancellationToken ct = default);
}
