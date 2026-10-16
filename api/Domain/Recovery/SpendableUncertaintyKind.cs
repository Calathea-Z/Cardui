namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// Why a spendable estimate is incomplete.
/// The member name is the stored text and the JSON text.
/// </summary>
public enum SpendableUncertaintyKind
{
    VariableIncome,
    UncertainIncome,
    NoReliableIncome,
    OrdinarySpendingUnset,
    WindfallLeftOut,
    IncomeLeftOut,
    ExcludedCurrency,
    DebtLeftOut
}
