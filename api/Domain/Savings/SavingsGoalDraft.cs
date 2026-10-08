using Cardui.Api.Models;

namespace Cardui.Api.Domain.Savings;

/// <summary>
/// One valid savings row after the amounts and kind are checked.
/// ReservedAmount is available now, or the amount set aside on a goal that finishes. Zero means nothing was typed.
/// TargetAmount and TargetDate are set for a goal that finishes. MonthlyAmount and ReadyDay are set for monthly living spending.
/// FloorAmount is the cash to always keep.
/// </summary>
public readonly record struct SavingsGoalDraft(
    SavingsGoalKind Kind,
    string Name,
    decimal? TargetAmount,
    DateOnly? TargetDate,
    decimal ReservedAmount,
    Guid? AccountId,
    bool UseAccountBalance,
    decimal? MonthlyAmount,
    int? ReadyDay,
    decimal? FloorAmount);
