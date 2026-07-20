using Cardui.Api.Dtos.Dashboard;

namespace Cardui.Api.Services.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(
        CancellationToken cancellationToken = default);
}
