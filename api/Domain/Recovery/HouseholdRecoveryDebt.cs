using Cardui.Api.Models;

namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// One household debt as the plan reads it.
/// BalanceInUse is the amount owed, including a followed card. A null balance is unknown and is not treated as zero.
/// CreditLimit is the limit in use. A blank limit leaves utilization unknown.
/// A blank rate, minimum, or due date stays unknown.
/// </summary>
public sealed record HouseholdRecoveryDebt(
    Guid DebtId,
    string Name,
    string Currency,
    DebtKind Kind,
    decimal? BalanceInUse,
    decimal? Apr,
    decimal? PromotionalApr,
    DateOnly? PromotionalEndsOn,
    decimal? MinimumPayment,
    int? RemainingTermMonths,
    DateOnly? NextDueDate,
    decimal? CreditLimit);
