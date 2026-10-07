using Cardui.Api.Domain.Debts;

namespace Cardui.Api.Dtos.Debts;

public class DebtBalanceComparisonDto
{
    public decimal AccountBalance { get; set; }

    public DateOnly? AccountBalanceAsOf { get; set; }

    /// <summary>
    /// Currency of the linked account balance. Null when the account does not say.
    /// </summary>
    public string? AccountCurrency { get; set; }

    /// <summary>
    /// True when the person can store this account balance on the debt.
    /// The plan keeps the debt's dated balance until they do.
    /// </summary>
    public bool CanUseAccountBalance { get; set; }

    public DebtAccountBalanceBlock Block { get; set; }
}
