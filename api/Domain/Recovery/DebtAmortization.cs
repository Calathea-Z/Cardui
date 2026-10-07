using Cardui.Api.Domain.Accounts;
using Cardui.Api.Domain.Debts;

namespace Cardui.Api.Domain.Recovery;

public static class DebtAmortization
{
    /// <summary>
    /// Projects one debt from its next due date, month by month.
    /// Each payment charges one month of simple interest, then pays the fixed minimum plus this debt's extra.
    /// The projection stops when the balance is gone, a payment does not reduce it, a later rate is unknown,
    /// or the date window or 600 month cap is reached. A freed minimum is not rolled to another debt.
    /// Through is the last due date to include. A null through runs until the projection stops.
    /// </summary>
    public static DebtSchedule Project(DebtAmortizationInput input, DateOnly? through = null)
    {
        var opening = DebtPaymentFacts.Resolve(input);
        if (!opening.IsResolved)
        {
            return Empty(input, opening.Balance, opening.Skip!.Value);
        }

        if (through is DateOnly end && opening.DueDate > end)
        {
            return Empty(input, opening.Balance, DebtScheduleStop.HorizonReached);
        }

        return ProjectPeriods(input, opening, through);
    }

    #region Private Methods

    /// <summary>
    /// Walks due dates until the debt is paid off or the projection has to stop.
    /// The minimum stays the opening minimum. The rate is read again on each due date.
    /// </summary>
    private static DebtSchedule ProjectPeriods(
        DebtAmortizationInput input,
        ResolvedDebtPayment opening,
        DateOnly? through)
    {
        var periods = new List<DebtPeriod>();
        var balance = opening.Balance;
        var extra = input.ExtraPayment <= 0 ? 0 : AccountLedger.Round(input.ExtraPayment);
        for (var index = 0; index < DebtRules.MaxRemainingTermMonths; index++)
        {
            if (!TryDueDate(opening.DueDate, index, out var due))
            {
                return Finish(input, periods, balance, DebtScheduleStop.HorizonReached);
            }

            if (due > DebtRules.LatestDate || (through is DateOnly end && due > end))
            {
                return Finish(input, periods, balance, DebtScheduleStop.HorizonReached);
            }

            var (rate, promotional) = DebtRate.InEffect(
                input.Apr,
                input.PromotionalApr,
                input.PromotionalEndsOn,
                due);
            if (rate is not decimal ratePercent)
            {
                return Finish(input, periods, balance, DebtScheduleStop.RateUnknown);
            }

            var period = DebtPeriodCalculator.Calculate(
                due,
                balance,
                ratePercent,
                promotional,
                opening.Minimum,
                extra);
            periods.Add(period);
            if (period.EndingBalance <= 0)
            {
                return Finish(input, periods, 0, DebtScheduleStop.PaidOff);
            }

            if (period.EndingBalance >= period.StartingBalance)
            {
                return Finish(input, periods, period.EndingBalance, DebtScheduleStop.DoesNotPayDown);
            }

            balance = period.EndingBalance;
        }

        return Finish(input, periods, balance, DebtScheduleStop.HorizonReached);
    }

    /// <summary>
    /// The due date that many months after the first, counted from the first date each time.
    /// A month that cannot be represented ends the projection.
    /// </summary>
    private static bool TryDueDate(DateOnly firstDue, int monthsLater, out DateOnly due)
    {
        try
        {
            due = firstDue.AddMonths(monthsLater);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            due = default;
            return false;
        }
    }

    /// <summary>
    /// A schedule with no payments.
    /// </summary>
    private static DebtSchedule Empty(
        DebtAmortizationInput input,
        decimal ending,
        DebtScheduleStop stop)
    {
        return Finish(input, [], ending, stop);
    }

    /// <summary>
    /// Assembles the schedule from the periods already produced.
    /// </summary>
    private static DebtSchedule Finish(
        DebtAmortizationInput input,
        IReadOnlyList<DebtPeriod> periods,
        decimal ending,
        DebtScheduleStop stop)
    {
        return new DebtSchedule(
            input.DebtId,
            input.Name,
            input.Currency,
            stop,
            ending,
            periods);
    }

    #endregion
}
