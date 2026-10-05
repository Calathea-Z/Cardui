namespace Cardui.Api.Domain;

/// <summary>
/// One expected raise: the date it starts, and the typical net pay for a single payment from then on.
/// IncomeSourceRules accepts or rejects it. The source's current amount stays unchanged.
/// </summary>
public readonly record struct IncomeRaiseDraft(
    DateOnly EffectiveDate,
    decimal TakeHomeAmount);
