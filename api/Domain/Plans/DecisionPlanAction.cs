namespace Cardui.Api.Domain.Plans;

/// <summary>
/// One ranked action in a decision plan.
/// Order starts at 1. Name is a debt or other label when the action has one.
/// Amount is the payment, shortfall, or windfall the action is about. Extra is shared extra or freed breathing room.
/// Count is a number of debts, balances, or months. For <see cref="DecisionPlanActionKind.LowerExtra"/> it is 1 for ordinary spending, 2 for a near-term bill or debt payment, and 3 for both.
/// AlreadyShortWithoutExtra is true only for that extra-payment action, when the horizon is short even without the extra.
/// </summary>
public sealed record DecisionPlanAction(
    int Order,
    DecisionPlanActionKind Kind,
    string? Name,
    string? NextName,
    decimal? Amount,
    decimal? Extra,
    DateOnly? On,
    int? Count,
    bool AlreadyShortWithoutExtra);
