using Cardui.Api.Domain.Accounts;
using Cardui.Api.Domain.Debts;
using Cardui.Api.Models;

namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// Mutable month-by-month state for one debt inside a payoff projection.
/// The projection owns it. Callers do not see it.
/// </summary>
internal sealed class PayoffRun
{
    public PayoffRun(PayoffDebt debt, ResolvedDebtPayment opening, int avalancheIndex)
    {
        Debt = debt;
        Opening = opening;
        AvalancheIndex = avalancheIndex;
        Balance = opening.Balance;
        OwnExtra = debt.Terms.ExtraPayment <= 0 ? 0 : AccountLedger.Round(debt.Terms.ExtraPayment);
        WasHigh = IsHigh(opening.Balance);
        EndingUtilization = OpeningUtilization();
        if (!opening.IsResolved)
        {
            Stop = opening.Skip;
            Finished = true;
        }
    }

    public PayoffDebt Debt { get; }

    public ResolvedDebtPayment Opening { get; }

    public int AvalancheIndex { get; }

    public decimal Balance { get; set; }

    public decimal OwnExtra { get; }

    public decimal Interest { get; set; }

    public bool WasHigh { get; }

    public bool Finished { get; set; }

    public DebtScheduleStop? Stop { get; set; }

    public DateOnly? PaidOffOn { get; set; }

    public int? PaymentsUntilPaidOff { get; set; }

    public int? PaymentsUntilUnderLimit { get; set; }

    public decimal? EndingUtilization { get; set; }

    public Guid DebtId => Debt.Terms.DebtId;

    /// <summary>
    /// True when this revolving balance is still at or above the limit notice.
    /// A paid-off debt, an installment, and an unknown limit are not.
    /// </summary>
    public bool StillAtLimit()
    {
        return !Finished && IsHigh(Balance);
    }

    #region Private Methods

    /// <summary>
    /// True when this revolving debt's balance is at or above 90 percent of its limit.
    /// </summary>
    private bool IsHigh(decimal balance)
    {
        if (Debt.Terms.Kind != DebtKind.Revolving)
        {
            return false;
        }

        var utilization = DebtRules.Utilization(balance, Debt.CreditLimit);
        return utilization is decimal value && value >= DebtSummary.UtilizationLimitNotice;
    }

    /// <summary>
    /// The opening utilization for a revolving debt. An installment stays unknown.
    /// </summary>
    private decimal? OpeningUtilization()
    {
        if (Debt.Terms.Kind != DebtKind.Revolving)
        {
            return null;
        }

        return DebtRules.Utilization(Opening.Balance, Debt.CreditLimit);
    }

    #endregion
}
