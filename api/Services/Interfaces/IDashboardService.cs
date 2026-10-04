using Cardui.Api.Dtos.Dashboard;

namespace Cardui.Api.Services.Interfaces;

public interface IDashboardService
{
    /// <summary>
    /// Builds the dashboard for the current month through today: cash,
    /// credit cards, net worth, income, spending, recent transactions, and
    /// spending by category. Transfers and balance reconciliations are excluded
    /// from income and spending.
    /// </summary>
    Task<DashboardSummaryDto> GetSummaryAsync(
        CancellationToken cancellationToken = default);
}
