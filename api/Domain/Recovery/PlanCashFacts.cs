using Cardui.Api.Domain.Accounts;

namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// The existing household facts Plan needs in addition to debt terms.
/// Savings setup flags distinguish a missing foundation from a saved zero current amount.
/// </summary>
internal sealed record PlanCashFacts(
    HouseholdCashOutlookInput Input,
    CashPosition CashPosition,
    decimal LivingSpendingMonthly,
    bool HasCashFloor,
    bool HasEmergencyGoal,
    int NamedSavingsGoalCount);
