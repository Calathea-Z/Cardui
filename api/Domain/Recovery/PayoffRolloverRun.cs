using Cardui.Api.Domain.Accounts;

namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// Mutable month-by-month state for one debt inside a rollover projection.
/// The projection owns it. Callers do not see it.
/// </summary>
internal sealed class PayoffRolloverRun
{
    public PayoffRolloverRun(PayoffDebt debt)
    {
        Debt = debt;
        Opening = DebtPaymentFacts.Resolve(debt.Terms);
        Balance = Opening.Balance;
        OwnExtra = debt.Terms.ExtraPayment <= 0 ? 0 : AccountLedger.Round(debt.Terms.ExtraPayment);
        if (!Opening.IsResolved)
        {
            Stop = Opening.Skip;
            Finished = true;
        }
    }

    public PayoffDebt Debt { get; }

    public ResolvedDebtPayment Opening { get; }

    public decimal Balance { get; set; }

    public decimal OwnExtra { get; }

    public decimal Interest { get; set; }

    public bool Finished { get; set; }

    public DebtScheduleStop? Stop { get; set; }

    public DateOnly? PaidOffOn { get; set; }

    public int? PaymentsUntilPaidOff { get; set; }

    public decimal? EndingUtilization { get; set; }

    public Guid DebtId => Debt.Terms.DebtId;
}
