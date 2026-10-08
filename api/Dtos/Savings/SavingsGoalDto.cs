using Cardui.Api.Models;

namespace Cardui.Api.Dtos.Savings;

public sealed class SavingsGoalDto
{
    public Guid Id { get; set; }

    public SavingsGoalKind Kind { get; set; }

    public required string Name { get; set; }

    /// <summary>
    /// The amount to reach by the target date. Null for everyday spending and cash to keep.
    /// </summary>
    public decimal? TargetAmount { get; set; }

    /// <summary>
    /// The date a finishing goal is funded. Null for everyday spending and cash to keep.
    /// </summary>
    public DateOnly? TargetDate { get; set; }

    /// <summary>
    /// How much everyday spending leaves cash each month. Null for every other kind.
    /// </summary>
    public decimal? MonthlyAmount { get; set; }

    /// <summary>
    /// The day of the month everyday spending counts. Null for every other kind.
    /// </summary>
    public int? ReadyDay { get; set; }

    /// <summary>
    /// The cash floor the plan protects. Null for every other kind.
    /// </summary>
    public decimal? FloorAmount { get; set; }

    /// <summary>
    /// The amount the person typed. Zero means nothing was typed.
    /// </summary>
    public decimal ReservedAmount { get; set; }

    /// <summary>
    /// The amount the plan protects: the account balance while following, otherwise the typed amount.
    /// </summary>
    public decimal AmountInUse { get; set; }

    public required string Currency { get; set; }

    public Guid? AccountId { get; set; }

    public string? AccountName { get; set; }

    public string? AccountMask { get; set; }

    /// <summary>
    /// The followed account's balance. Null when the goal does not follow an account.
    /// </summary>
    public decimal? AccountBalance { get; set; }

    /// <summary>
    /// True when the plan is using an eligible account's balance or an override of it.
    /// </summary>
    public bool Following { get; set; }

    /// <summary>
    /// True when the person is keeping their own amount instead of the account balance.
    /// </summary>
    public bool ReservedOverridden { get; set; }

    /// <summary>
    /// True when a stored account can no longer be followed. The typed amount is what the plan uses.
    /// </summary>
    public bool AccountUnavailable { get; set; }

    /// <summary>
    /// True when the followed balance is below zero, so nothing is set aside from it.
    /// </summary>
    public bool NegativeBalance { get; set; }

    /// <summary>
    /// The gap between the target and the amount in use. Zero when the goal is funded.
    /// </summary>
    public decimal Remaining { get; set; }

    public bool AlreadyMet { get; set; }

    /// <summary>
    /// True when the target date is already past, so the gap is due now.
    /// </summary>
    public bool DatePassed { get; set; }

    /// <summary>
    /// True when the target date is too far out to schedule.
    /// </summary>
    public bool BeyondHorizon { get; set; }

    /// <summary>
    /// The monthly amount that hits the date. Null when the goal is funded, the date has passed, or it is too far out.
    /// </summary>
    public decimal? AmountNeededPerMonth { get; set; }

    /// <summary>
    /// The last month's amount when it differs from the monthly amount. Null in the same cases as AmountNeededPerMonth.
    /// </summary>
    public decimal? FinalAmountNeeded { get; set; }
}
