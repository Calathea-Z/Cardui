using Cardui.Api.Dtos.Plan;

namespace Cardui.Api.Services.Interfaces;

public interface IPlanService
{
    /// <summary>
    /// Returns the household's payoff on rollover and on keeping every freed payment.
    /// Each path carries its payoff steps, each debt's outcome, and the balance after every payment.
    /// The balance and credit limit already in use on each debt are the amounts owed. A debt with no balance is listed apart.
    /// The query is a temporary preview. Monthly extra is shared extra aimed at the first debt that can take it. Zero is minimums only.
    /// Income loss leaves out included pay for that many months. A windfall adds cash on one date and does not pay a debt.
    /// Flexible spending and the cash floor replace those saved amounts in the preview only. Null keeps the saved amount.
    /// A custom order and a reclaim amount stay at zero because those choices are not stored yet.
    /// Payments start today: a stored due date that has passed is stepped to its next monthly date on or after today.
    /// The cash outlook starts from the Cash total on Accounts, adds income and bills, and pays debts as each path does,
    /// at typical pay and, when recorded, low pay. The extra is part of those debt payments.
    /// Nothing is saved.
    /// </summary>
    Task<PlanRecoveryDto> GetRecoveryAsync(
        PlanScenarioQuery scenario,
        CancellationToken cancellationToken = default);
}
