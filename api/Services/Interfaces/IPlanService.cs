using Cardui.Api.Dtos.Plan;

namespace Cardui.Api.Services.Interfaces;

public interface IPlanService
{
    /// <summary>
    /// Returns the household's cash-flow recovery.
    /// The balance and credit limit already in use on each debt are the amounts owed.
    /// Shared extra, a custom order, and a reclaim amount stay at zero because those choices are not stored yet.
    /// Nothing is saved.
    /// </summary>
    Task<CashFlowRecoveryReportDto> GetRecoveryAsync(
        CancellationToken cancellationToken = default);
}
