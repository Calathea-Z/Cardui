namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// One reason the spendable estimate may be high or incomplete.
/// Name is the income, debt, or currency it refers to. Empty when the reason has no subject.
/// </summary>
public sealed record SpendableUncertainty(
    SpendableUncertaintyKind Kind,
    string Name);
