namespace Cardui.Api.Domain.Debts;

/// <summary>
/// A linked liability balance that is not the same amount as the debt.
/// The debt's own balance stays the one used for interest and utilization.
/// CanUseAccountBalance is true only when that account balance has a date, a matching currency, and an amount the debt can store.
/// </summary>
public sealed record DebtBalanceComparison(
    decimal AccountBalance,
    DateOnly? AccountBalanceAsOf,
    string? AccountCurrency,
    bool CanUseAccountBalance,
    DebtAccountBalanceBlock Block);
