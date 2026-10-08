using Cardui.Api.Models;

namespace Cardui.Api.Domain.Savings;

/// <summary>
/// One savings row the outlook can read.
/// AmountInUse is available now, or the amount set aside on a goal that finishes. Currency may differ from the plan.
/// MonthlyAmount and ReadyDay are set for everyday spending. FloorAmount is the cash the plan always protects.
/// TargetAmount is zero and TargetDate is empty when this row does not finish on a date.
/// </summary>
public sealed record SavingsGoalSnapshot(
    Guid Id,
    string Name,
    string Currency,
    decimal TargetAmount,
    DateOnly TargetDate,
    decimal AmountInUse,
    SavingsGoalKind Kind = SavingsGoalKind.Emergency,
    decimal MonthlyAmount = 0m,
    int ReadyDay = 1,
    decimal FloorAmount = 0m);
