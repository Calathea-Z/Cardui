using Cardui.Api.Domain.Income;
using Cardui.Api.Domain.Obligations;
using Cardui.Api.Models;

namespace Cardui.Api.Domain.Recovery;

public static class CashFlowSchedule
{
    /// <summary>
    /// Places income and bills on their own dates, from the start through the end, inclusive.
    /// Repeating dates use the same steps as paycheck and bill schedules. Irregular items appear once.
    /// An income raise changes the payment on and after its date. Amounts are not averaged onto one day.
    /// The list is ordered by date, then income, bills, debt payments, and savings, then name.
    /// </summary>
    public static IReadOnlyList<CashFlowEvent> Project(
        IReadOnlyList<DatedIncome> incomes,
        IReadOnlyList<DatedBill> bills,
        DateOnly from,
        DateOnly through)
    {
        if (through < from)
        {
            return [];
        }

        var events = new List<CashFlowEvent>();
        foreach (var income in incomes)
        {
            AddIncome(events, income, from, through);
        }

        foreach (var bill in bills)
        {
            AddBill(events, bill, from, through);
        }

        return Sort(events);
    }

    /// <summary>
    /// Turns each debt payment that moves cash into an event.
    /// A zero payment is omitted. The debt schedule still reports that period.
    /// </summary>
    public static IReadOnlyList<CashFlowEvent> DebtPayments(DebtSchedule schedule)
    {
        var events = new List<CashFlowEvent>();
        foreach (var period in schedule.Periods)
        {
            if (period.Payment <= 0)
            {
                continue;
            }

            events.Add(new CashFlowEvent(
                period.DueDate,
                CashFlowKind.DebtPayment,
                schedule.DebtId,
                schedule.Name,
                period.Payment,
                schedule.Currency));
        }

        return events;
    }

    /// <summary>
    /// Turns savings contributions into events.
    /// A contribution reserves cash. It is not a bill.
    /// </summary>
    public static IReadOnlyList<CashFlowEvent> Savings(
        Guid sourceId,
        string name,
        string currency,
        IReadOnlyList<SavingsContribution> contributions)
    {
        var events = new List<CashFlowEvent>();
        foreach (var contribution in contributions)
        {
            if (contribution.Amount <= 0)
            {
                continue;
            }

            events.Add(new CashFlowEvent(
                contribution.Date,
                CashFlowKind.Savings,
                sourceId,
                name,
                contribution.Amount,
                currency));
        }

        return events;
    }

    /// <summary>
    /// Orders any cash events the same way a projection is ordered.
    /// Same-day income comes first, so a bill due on payday is listed after that pay.
    /// </summary>
    public static IReadOnlyList<CashFlowEvent> Combine(IEnumerable<CashFlowEvent> events)
    {
        return Sort(events.ToList());
    }

    #region Private Methods

    /// <summary>
    /// Adds one income source's payments inside the window.
    /// A raise already in effect on a date replaces the payment. Dates after the stored latest date are omitted.
    /// </summary>
    private static void AddIncome(
        List<CashFlowEvent> events,
        DatedIncome income,
        DateOnly from,
        DateOnly through)
    {
        var end = through > IncomeSourceRules.LatestPaymentDate
            ? IncomeSourceRules.LatestPaymentDate
            : through;
        if (income.Cadence == IncomeCadence.Irregular)
        {
            AddOnce(
                events,
                income.NextPaymentDate,
                from,
                end,
                new CashFlowEvent(
                    income.NextPaymentDate,
                    CashFlowKind.Income,
                    income.Id,
                    income.Name,
                    AmountOn(income, income.NextPaymentDate),
                    income.Currency));
            return;
        }

        if (!CadenceDates.TryStep(income.Cadence, out var step))
        {
            return;
        }

        foreach (var date in CadenceDates.Enumerate(step, income.NextPaymentDate, end))
        {
            if (date < from)
            {
                continue;
            }

            events.Add(new CashFlowEvent(
                date,
                CashFlowKind.Income,
                income.Id,
                income.Name,
                AmountOn(income, date),
                income.Currency));
        }
    }

    /// <summary>
    /// Adds one bill's due dates inside the window.
    /// Irregular appears once. Dates after the stored latest due date are omitted.
    /// </summary>
    private static void AddBill(
        List<CashFlowEvent> events,
        DatedBill bill,
        DateOnly from,
        DateOnly through)
    {
        var end = through > ObligationRules.LatestDueDate
            ? ObligationRules.LatestDueDate
            : through;
        if (bill.Cadence == ObligationCadence.Irregular)
        {
            AddOnce(
                events,
                bill.NextDueDate,
                from,
                end,
                new CashFlowEvent(
                    bill.NextDueDate,
                    CashFlowKind.Bill,
                    bill.Id,
                    bill.Name,
                    bill.Amount,
                    bill.Currency));
            return;
        }

        if (!CadenceDates.TryStep(bill.Cadence, out var step))
        {
            return;
        }

        foreach (var date in CadenceDates.Enumerate(step, bill.NextDueDate, end))
        {
            if (date < from)
            {
                continue;
            }

            events.Add(new CashFlowEvent(
                date,
                CashFlowKind.Bill,
                bill.Id,
                bill.Name,
                bill.Amount,
                bill.Currency));
        }
    }

    /// <summary>
    /// Adds a single known date when it falls inside the window.
    /// </summary>
    private static void AddOnce(
        List<CashFlowEvent> events,
        DateOnly date,
        DateOnly from,
        DateOnly through,
        CashFlowEvent item)
    {
        if (date >= from && date <= through)
        {
            events.Add(item);
        }
    }

    /// <summary>
    /// The payment amount on a date after any raise already in effect.
    /// Raises are applied in effective-date order. A later raise replaces an earlier one.
    /// </summary>
    private static decimal AmountOn(DatedIncome income, DateOnly date)
    {
        var amount = income.Amount;
        foreach (var raise in income.Raises.OrderBy(raise => raise.EffectiveDate))
        {
            if (raise.EffectiveDate <= date)
            {
                amount = raise.Amount;
            }
        }

        return amount;
    }

    /// <summary>
    /// Orders events so the same inputs always produce the same list.
    /// Income is first on a date, then bills, debt payments, and savings.
    /// </summary>
    private static IReadOnlyList<CashFlowEvent> Sort(List<CashFlowEvent> events)
    {
        return events
            .OrderBy(item => item.Date)
            .ThenBy(item => KindRank(item.Kind))
            .ThenBy(item => item.Name, StringComparer.Ordinal)
            .ThenBy(item => item.SourceId)
            .ToList();
    }

    /// <summary>
    /// The order of kinds on one date.
    /// Income can cover a bill due the same day, so it is listed first.
    /// </summary>
    private static int KindRank(CashFlowKind kind)
    {
        return kind switch
        {
            CashFlowKind.Income => 0,
            CashFlowKind.Bill => 1,
            CashFlowKind.DebtPayment => 2,
            CashFlowKind.Savings => 3,
            CashFlowKind.LivingSpending => 4,
            _ => 5
        };
    }

    #endregion
}
