using Cardui.Api.Domain.Recovery;

namespace Cardui.Api.Domain.Plans;

/// <summary>
/// One reason spendable cash left something out of this plan.
/// Name is empty when the reason is not a particular income, debt, or currency.
/// </summary>
public sealed record DecisionPlanUncertainty(
    SpendableUncertaintyKind Kind,
    string Name);
