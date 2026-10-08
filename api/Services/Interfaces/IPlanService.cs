using Cardui.Api.Dtos.Plan;

namespace Cardui.Api.Services.Interfaces;

public interface IPlanService
{
    /// <summary>
    /// Returns the household's payoff on rollover and on keeping every freed payment.
    /// Each path carries its payoff steps, each debt's outcome, and the balance after every payment.
    /// The balance and credit limit already in use on each debt are the amounts owed. A debt with no balance is listed apart.
    /// Shared extra, a custom order, and a reclaim amount stay at zero because those choices are not stored yet.
    /// Nothing is saved.
    /// </summary>
    Task<PlanRecoveryDto> GetRecoveryAsync(
        CancellationToken cancellationToken = default);
}
