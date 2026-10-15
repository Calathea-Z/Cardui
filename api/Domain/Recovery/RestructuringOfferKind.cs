namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// The kind of offer being compared with debts the household already has.
/// A consolidation loan replaces two or more debts. A replacement loan replaces one.
/// A balance transfer replaces revolving debts only. The offer is not treated as approved.
/// </summary>
public enum RestructuringOfferKind
{
    ConsolidationLoan,
    BalanceTransfer,
    ReplacementLoan
}
