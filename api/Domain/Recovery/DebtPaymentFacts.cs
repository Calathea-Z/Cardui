using Cardui.Api.Domain.Accounts;
using Cardui.Api.Domain.Debts;

namespace Cardui.Api.Domain.Recovery;

public static class DebtPaymentFacts
{
    /// <summary>
    /// Resolves the balance, due date, rate, and minimum for the next payment.
    /// A balance at or below zero is already paid off. A missing date, rate, or minimum is left unknown.
    /// The rate is the rate in effect on the due date. The minimum is fixed from the opening balance.
    /// </summary>
    public static ResolvedDebtPayment Resolve(DebtAmortizationInput input)
    {
        var balance = AccountLedger.Round(input.Balance);
        if (balance <= 0)
        {
            return Skipped(DebtScheduleStop.PaidOff);
        }

        if (input.NextDueDate is not DateOnly due)
        {
            return Skipped(DebtScheduleStop.DueDateUnknown, balance);
        }

        var (rate, promotional) = DebtRate.InEffect(
            input.Apr,
            input.PromotionalApr,
            input.PromotionalEndsOn,
            due);
        if (rate is not decimal ratePercent)
        {
            return Skipped(DebtScheduleStop.RateUnknown, balance);
        }

        var minimum = DebtMinimum.Resolve(
            input.Kind,
            balance,
            ratePercent,
            input.MinimumPayment,
            input.RemainingTermMonths);
        if (minimum is not decimal minimumAmount)
        {
            return Skipped(DebtScheduleStop.MinimumUnknown, balance);
        }

        return new ResolvedDebtPayment(
            null,
            balance,
            due,
            ratePercent,
            promotional,
            minimumAmount);
    }

    #region Private Methods

    /// <summary>
    /// A payment that cannot be calculated.
    /// Balance is the cents still owed when that is known, and zero when the debt is already paid off.
    /// </summary>
    private static ResolvedDebtPayment Skipped(DebtScheduleStop stop, decimal balance = 0)
    {
        return new ResolvedDebtPayment(stop, balance, default, 0, false, 0);
    }

    #endregion
}
