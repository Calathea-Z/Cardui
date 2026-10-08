using Cardui.Api.Domain.Accounts;

namespace Cardui.Api.Domain.Recovery;

public static class CashForecast
{
    private const int DayCount = 30;

    private static readonly int[] HorizonMonths = [6, 12, 18];

    /// <summary>
    /// Builds a 30-day cash view and 6, 12, and 18 month forecasts.
    /// Income, bills, debt payments, and savings stay on their dates. Amounts are not averaged.
    /// Cash changes with income, bills, and debt payments. A savings contribution adds to the reserve and leaves cash as it is.
    /// Available cash is cash minus the reserve. A shortfall stays negative.
    /// Debt payments before the start are not replayed. Another currency is left out.
    /// A schedule given for a debt replaces that debt's own amortization, so a payoff path can roll a freed payment
    /// into another debt. Those schedules must start on or after AsOf. A debt with no schedule pays its own minimum and extra.
    /// </summary>
    public static CashForecastReport Project(
        CashForecastInput input,
        IReadOnlyList<DebtSchedule>? debtSchedules = null)
    {
        var through = WalkThrough(input.AsOf);
        var paths = ProjectDebts(input, through, debtSchedules);
        var events = Events(input, paths, through);
        var walked = Walk(input, events, through);
        var days = FirstDays(walked);
        var excluded = Excluded(input);
        return new CashForecastReport(
            input.PlanningCurrency,
            input.IncomeBasis,
            input.AsOf,
            Summarize(input.AsOf, days[^1].Date, days),
            days,
            Horizons(input.AsOf, walked, paths),
            Milestones(walked, paths),
            excluded,
            Assumptions(input, excluded, debtSchedules is not null));
    }

    #region Private Methods

    /// <summary>
    /// The last day to walk. Eighteen months when that date exists, otherwise the longest horizon that does.
    /// </summary>
    private static DateOnly WalkThrough(DateOnly asOf)
    {
        for (var index = HorizonMonths.Length - 1; index >= 0; index--)
        {
            if (TryHorizonEnd(asOf, HorizonMonths[index], out var end))
            {
                return end;
            }
        }

        return TryAddDays(asOf, DayCount - 1, out var days) ? days : asOf;
    }

    /// <summary>
    /// The last day of a horizon that starts on asOf.
    /// Six months from January 1 ends on June 30.
    /// </summary>
    private static bool TryHorizonEnd(DateOnly asOf, int months, out DateOnly end)
    {
        try
        {
            end = asOf.AddMonths(months).AddDays(-1);
            return end >= asOf;
        }
        catch (ArgumentOutOfRangeException)
        {
            end = default;
            return false;
        }
    }

    /// <summary>
    /// A date that many days after the start.
    /// False when that date cannot be represented.
    /// </summary>
    private static bool TryAddDays(DateOnly asOf, int days, out DateOnly end)
    {
        try
        {
            end = asOf.AddDays(days);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            end = default;
            return false;
        }
    }

    /// <summary>
    /// Pairs each debt in the planning currency with its schedule from the forecast start through the walk.
    /// A given schedule is used as it is. Otherwise the debt is amortized on its own. A debt in another currency is left out.
    /// </summary>
    private static List<ForecastDebtPath> ProjectDebts(
        CashForecastInput input,
        DateOnly through,
        IReadOnlyList<DebtSchedule>? debtSchedules)
    {
        var given = (debtSchedules ?? [])
            .GroupBy(schedule => schedule.DebtId)
            .ToDictionary(group => group.Key, group => group.First());
        var paths = new List<ForecastDebtPath>();
        foreach (var debt in input.Debts)
        {
            if (!PlanningCurrencyRules.IsIncluded(debt.Currency, input.PlanningCurrency))
            {
                continue;
            }

            paths.Add(new ForecastDebtPath(
                debt,
                given.GetValueOrDefault(debt.DebtId)
                    ?? DebtAmortization.Project(debt, through, input.AsOf)));
        }

        return paths;
    }

    /// <summary>
    /// The dated amounts to apply, in income, bill, debt, then savings order.
    /// Rows in another currency are omitted. A savings row that is not a savings event is omitted.
    /// </summary>
    private static IReadOnlyList<CashFlowEvent> Events(
        CashForecastInput input,
        IReadOnlyList<ForecastDebtPath> paths,
        DateOnly through)
    {
        var incomes = input.Incomes
            .Where(income => Included(income.Currency, input.PlanningCurrency))
            .ToList();
        var bills = input.Bills
            .Where(bill => Included(bill.Currency, input.PlanningCurrency))
            .ToList();
        var flow = CashFlowSchedule.Project(incomes, bills, input.AsOf, through);
        var payments = paths.SelectMany(path => CashFlowSchedule.DebtPayments(path.Schedule));
        var savings = input.Savings.Where(item =>
            item.Kind is CashFlowKind.Savings or CashFlowKind.LivingSpending
            && item.Amount > 0
            && item.Date >= input.AsOf
            && item.Date <= through
            && Included(item.Currency, input.PlanningCurrency));
        return CashFlowSchedule.Combine(flow.Concat(payments).Concat(savings));
    }

    /// <summary>
    /// True when a row counts in the planning currency.
    /// A blank currency counts. Another currency does not.
    /// </summary>
    private static bool Included(string? currency, string planningCurrency)
    {
        return PlanningCurrencyRules.IsIncluded(currency, planningCurrency);
    }

    /// <summary>
    /// Applies events day by day from the start through the last horizon.
    /// A day with no events keeps the prior cash and reserve.
    /// </summary>
    private static List<CashDay> Walk(
        CashForecastInput input,
        IReadOnlyList<CashFlowEvent> events,
        DateOnly through)
    {
        var cash = AccountLedger.Round(input.StartingCash);
        var reserve = input.StartingReserve <= 0 ? 0 : AccountLedger.Round(input.StartingReserve);
        var byDate = events.ToLookup(item => item.Date);
        var days = new List<CashDay>();
        for (var date = input.AsOf; date <= through; date = date.AddDays(1))
        {
            var todays = byDate[date].ToList();
            foreach (var item in todays)
            {
                Apply(ref cash, ref reserve, item);
            }

            days.Add(CloseDay(date, cash, reserve, todays));
            if (date == DateOnly.MaxValue)
            {
                break;
            }
        }

        return days;
    }

    /// <summary>
    /// Applies one event to cash or the reserve.
    /// Income adds cash. A bill or debt payment removes cash. Savings adds to the reserve only.
    /// </summary>
    private static void Apply(ref decimal cash, ref decimal reserve, CashFlowEvent item)
    {
        if (item.Amount <= 0)
        {
            return;
        }

        switch (item.Kind)
        {
            case CashFlowKind.Income:
                cash = AccountLedger.Round(cash + item.Amount);
                break;
            case CashFlowKind.Bill:
            case CashFlowKind.DebtPayment:
            case CashFlowKind.LivingSpending:
                cash = AccountLedger.Round(cash - item.Amount);
                break;
            case CashFlowKind.Savings:
                reserve = AccountLedger.Round(reserve + item.Amount);
                break;
        }
    }

    /// <summary>
    /// The ending position for one day.
    /// Available below zero is a reserve shortfall only when a reserve is actually set aside.
    /// </summary>
    private static CashDay CloseDay(
        DateOnly date,
        decimal cash,
        decimal reserve,
        IReadOnlyList<CashFlowEvent> events)
    {
        var available = AccountLedger.Round(cash - reserve);
        return new CashDay(
            date,
            cash,
            available,
            reserve,
            cash < 0,
            available < 0 && reserve > 0,
            events);
    }

    /// <summary>
    /// The first 30 walked days, or every day when the calendar ends sooner.
    /// </summary>
    private static List<CashDay> FirstDays(List<CashDay> walked)
    {
        var count = Math.Min(DayCount, walked.Count);
        return walked.GetRange(0, count);
    }

    /// <summary>
    /// One snapshot at each horizon the calendar can represent.
    /// </summary>
    private static List<CashHorizon> Horizons(
        DateOnly asOf,
        IReadOnlyList<CashDay> walked,
        IReadOnlyList<ForecastDebtPath> paths)
    {
        var horizons = new List<CashHorizon>();
        foreach (var months in HorizonMonths)
        {
            if (!TryHorizonEnd(asOf, months, out var end))
            {
                continue;
            }

            var slice = walked.Where(day => day.Date <= end).ToList();
            if (slice.Count == 0)
            {
                continue;
            }

            var debts = paths.Select(path => Describe(path, end)).ToList();
            var (obligation, unknown) = Obligations(debts);
            horizons.Add(new CashHorizon(
                months,
                Summarize(asOf, slice[^1].Date, slice),
                obligation,
                unknown,
                debts));
        }

        return horizons;
    }

    /// <summary>
    /// Ending cash, the lowest day, and whether any day in the stretch is short.
    /// </summary>
    private static CashWindow Summarize(DateOnly from, DateOnly through, IReadOnlyList<CashDay> days)
    {
        var lowest = Lowest(days);
        var end = days[^1];
        return new CashWindow(
            from,
            through,
            end.Cash,
            end.Available,
            end.Reserve,
            lowest.Cash,
            lowest.Date,
            days.Any(day => day.CashShortfall),
            days.Any(day => day.ReserveShortfall));
    }

    /// <summary>
    /// The day with the smallest cash. The earlier day wins a tie.
    /// </summary>
    private static CashDay Lowest(IReadOnlyList<CashDay> days)
    {
        var lowest = days[0];
        for (var index = 1; index < days.Count; index++)
        {
            if (days[index].Cash < lowest.Cash)
            {
                lowest = days[index];
            }
        }

        return lowest;
    }

    /// <summary>
    /// The balance and remaining minimum for one debt at a horizon.
    /// A debt already paid off before the forecast stays at zero and has no payoff date.
    /// </summary>
    private static ForecastDebtBalance Describe(ForecastDebtPath path, DateOnly through)
    {
        if (path.Schedule.Stop == DebtScheduleStop.PaidOff && path.Schedule.Periods.Count == 0)
        {
            return new ForecastDebtBalance(
                path.Input.DebtId,
                path.Input.Name,
                0,
                0,
                DebtScheduleStop.PaidOff,
                null);
        }

        var inRange = path.Schedule.Periods.Where(period => period.DueDate <= through).ToList();
        var stop = StopAsOf(path.Schedule, inRange, through);
        var balance = BalanceAsOf(path, inRange, stop);
        DateOnly? paidOffOn = stop == DebtScheduleStop.PaidOff && inRange.Count > 0
            ? inRange[^1].DueDate
            : null;
        return new ForecastDebtBalance(
            path.Input.DebtId,
            path.Input.Name,
            balance,
            MinimumStillDue(path, inRange, balance, stop),
            stop,
            paidOffOn);
    }

    /// <summary>
    /// Why the debt stands where it does at this horizon.
    /// A later stop, such as an unknown rate after this date, is not reported yet.
    /// </summary>
    private static DebtScheduleStop StopAsOf(
        DebtSchedule schedule,
        IReadOnlyList<DebtPeriod> inRange,
        DateOnly through)
    {
        if (inRange.Count > 0 && inRange[^1].EndingBalance <= 0)
        {
            return DebtScheduleStop.PaidOff;
        }

        if (inRange.Count > 0 && inRange[^1].EndingBalance >= inRange[^1].StartingBalance)
        {
            return DebtScheduleStop.DoesNotPayDown;
        }

        if (schedule.Stop is DebtScheduleStop.MinimumUnknown or DebtScheduleStop.DueDateUnknown)
        {
            return schedule.Stop;
        }

        if (schedule.Stop == DebtScheduleStop.RateUnknown
            && (schedule.Periods.Count == 0 || schedule.Periods[^1].DueDate <= through))
        {
            return DebtScheduleStop.RateUnknown;
        }

        return DebtScheduleStop.HorizonReached;
    }

    /// <summary>
    /// The balance after the last payment on or before the horizon.
    /// With no payment yet, it is the balance the forecast was given.
    /// </summary>
    private static decimal BalanceAsOf(
        ForecastDebtPath path,
        IReadOnlyList<DebtPeriod> inRange,
        DebtScheduleStop stop)
    {
        if (stop == DebtScheduleStop.PaidOff)
        {
            return 0;
        }

        if (inRange.Count > 0)
        {
            return inRange[^1].EndingBalance;
        }

        return AccountLedger.Round(path.Input.Balance);
    }

    /// <summary>
    /// The monthly minimum still due after the horizon.
    /// Zero when nothing remains. Null when a term required to keep paying is unknown.
    /// </summary>
    private static decimal? MinimumStillDue(
        ForecastDebtPath path,
        IReadOnlyList<DebtPeriod> inRange,
        decimal balance,
        DebtScheduleStop stop)
    {
        if (balance <= 0)
        {
            return 0;
        }

        if (stop is DebtScheduleStop.RateUnknown
            or DebtScheduleStop.MinimumUnknown
            or DebtScheduleStop.DueDateUnknown)
        {
            return null;
        }

        if (inRange.Count > 0)
        {
            return inRange[^1].MinimumPaid;
        }

        if (path.Schedule.Periods.Count > 0)
        {
            return path.Schedule.Periods[0].MinimumPaid;
        }

        var opening = DebtPaymentFacts.Resolve(path.Input);
        return opening.IsResolved ? opening.Minimum : null;
    }

    /// <summary>
    /// The sum of known minimums still due, and how many remaining debts could not be included.
    /// The sum is null when every remaining debt is unknown.
    /// </summary>
    private static (decimal? Obligation, int Unknown) Obligations(IReadOnlyList<ForecastDebtBalance> debts)
    {
        var unknown = 0;
        var known = 0m;
        var knownCount = 0;
        foreach (var debt in debts)
        {
            if (debt.Balance <= 0)
            {
                continue;
            }

            if (debt.MonthlyMinimum is not decimal minimum)
            {
                unknown++;
                continue;
            }

            known += minimum;
            knownCount++;
        }

        if (knownCount == 0 && unknown > 0)
        {
            return (null, unknown);
        }

        return (known, unknown);
    }

    /// <summary>
    /// Payoff dates and the first shortfall and recovery for cash and the reserve.
    /// Ordered by date, then kind, then name.
    /// </summary>
    private static List<ForecastMilestone> Milestones(
        IReadOnlyList<CashDay> days,
        IReadOnlyList<ForecastDebtPath> paths)
    {
        var milestones = new List<ForecastMilestone>();
        AddRunningMilestones(milestones, days);
        AddPayoffs(milestones, paths);
        return milestones
            .OrderBy(item => item.Date)
            .ThenBy(item => MilestoneRank(item.Kind))
            .ThenBy(item => item.Name, StringComparer.Ordinal)
            .ThenBy(item => item.SourceId)
            .ToList();
    }

    /// <summary>
    /// The first day cash or the reserve goes below zero, and the first later day it recovers.
    /// A reserve of zero does not produce a reserve shortfall.
    /// </summary>
    private static void AddRunningMilestones(
        List<ForecastMilestone> milestones,
        IReadOnlyList<CashDay> days)
    {
        var cashShort = false;
        var cashRecovered = false;
        var reserveShort = false;
        var reserveRestored = false;
        foreach (var day in days)
        {
            if (!cashShort && day.Cash < 0)
            {
                cashShort = true;
                milestones.Add(Point(day.Date, ForecastMilestoneKind.CashShortfall, null, "Cash", day.Cash));
            }
            else if (cashShort && !cashRecovered && day.Cash >= 0)
            {
                cashRecovered = true;
                milestones.Add(Point(day.Date, ForecastMilestoneKind.CashRecovered, null, "Cash", day.Cash));
            }

            if (!reserveShort && day.ReserveShortfall)
            {
                reserveShort = true;
                milestones.Add(Point(
                    day.Date,
                    ForecastMilestoneKind.ReserveShortfall,
                    null,
                    "Reserve",
                    day.Available));
            }
            else if (reserveShort && !reserveRestored && day.Available >= 0)
            {
                reserveRestored = true;
                milestones.Add(Point(
                    day.Date,
                    ForecastMilestoneKind.ReserveRestored,
                    null,
                    "Reserve",
                    day.Available));
            }
        }
    }

    /// <summary>
    /// One milestone for each debt the schedule pays off inside the walk.
    /// Amount is the monthly minimum that ends, from the first payment.
    /// A debt that was already paid off has no date in this forecast.
    /// </summary>
    private static void AddPayoffs(List<ForecastMilestone> milestones, IReadOnlyList<ForecastDebtPath> paths)
    {
        foreach (var path in paths)
        {
            if (path.Schedule.Stop != DebtScheduleStop.PaidOff || path.Schedule.Periods.Count == 0)
            {
                continue;
            }

            var payoff = path.Schedule.Periods[^1];
            if (payoff.EndingBalance > 0)
            {
                continue;
            }

            milestones.Add(Point(
                payoff.DueDate,
                ForecastMilestoneKind.DebtPaidOff,
                path.Input.DebtId,
                path.Input.Name,
                path.Schedule.Periods[0].MinimumPaid));
        }
    }

    /// <summary>
    /// One milestone row.
    /// </summary>
    private static ForecastMilestone Point(
        DateOnly date,
        ForecastMilestoneKind kind,
        Guid? sourceId,
        string name,
        decimal amount)
    {
        return new ForecastMilestone(date, kind, sourceId, name, amount);
    }

    /// <summary>
    /// The order of kinds on one date.
    /// A shortfall is listed before the payoff or recovery that shares its date.
    /// </summary>
    private static int MilestoneRank(ForecastMilestoneKind kind)
    {
        return kind switch
        {
            ForecastMilestoneKind.CashShortfall => 0,
            ForecastMilestoneKind.ReserveShortfall => 1,
            ForecastMilestoneKind.DebtPaidOff => 2,
            ForecastMilestoneKind.CashRecovered => 3,
            ForecastMilestoneKind.ReserveRestored => 4,
            _ => 5
        };
    }

    /// <summary>
    /// Currency codes that were left out, in code order.
    /// </summary>
    private static IReadOnlyList<string> Excluded(CashForecastInput input)
    {
        var codes = input.Incomes.Select(income => (string?)income.Currency)
            .Concat(input.Bills.Select(bill => (string?)bill.Currency))
            .Concat(input.Debts.Select(debt => (string?)debt.Currency))
            .Concat(input.Savings.Select(item => (string?)item.Currency));
        return PlanningCurrencyRules.ExcludedCodes(codes, input.PlanningCurrency);
    }

    /// <summary>
    /// The interest, payment, income, reserve, and currency rules this result used.
    /// The income sentence names the basis the caller supplied. The payment sentence says whether a payoff path was followed.
    /// </summary>
    private static IReadOnlyList<string> Assumptions(
        CashForecastInput input,
        IReadOnlyList<string> excluded,
        bool followsPath)
    {
        var assumptions = new List<string>
        {
            "Interest is one month of simple interest on the balance at the start of the period, rounded to cents away from zero. It is not an average daily balance.",
            followsPath
                ? "A debt payment is what the payoff path given to this forecast pays on that date, including a freed payment rolled into it. A payment that leaves the balance the same or higher stops that debt's schedule, and the higher balance stays visible."
                : "A debt payment is the stored minimum, or the level payment when an installment minimum is blank, plus extra recorded on that debt. Extra stays on that debt. A freed minimum is not rolled onto another debt. A payment that leaves the balance the same or higher stops that debt's schedule, and the higher balance stays visible.",
            "Income and bills stay on their dates. A repeating payment is not rewritten as a monthly average. A raise replaces the payment on and after its date.",
            BasisSentence(input.IncomeBasis),
            "A debt due date before the forecast start is not replayed. The next payment is the first monthly date on or after the start, and the balance stays the balance given until then.",
            "A savings contribution adds to the protected reserve. It does not reduce cash, and it is not a bill.",
            "Available cash is cash minus the protected reserve. A shortfall stays visible. The forecast does not add cash to cover it."
        };
        var currency = CurrencySentence(input.PlanningCurrency, excluded);
        if (currency is not null)
        {
            assumptions.Add(currency);
        }

        return assumptions;
    }

    /// <summary>
    /// Names the income basis the forecast was given.
    /// </summary>
    private static string BasisSentence(ForecastIncomeBasis basis)
    {
        return basis == ForecastIncomeBasis.Conservative
            ? "The income amounts are the conservative payments this forecast was given."
            : "The income amounts are the typical payments this forecast was given.";
    }

    /// <summary>
    /// The notice that another currency was left out.
    /// Null when every row counts in the planning currency.
    /// </summary>
    private static string? CurrencySentence(string planningCurrency, IReadOnlyList<string> excluded)
    {
        if (excluded.Count == 0)
        {
            return null;
        }

        return "Amounts in another currency are left out: "
            + string.Join(", ", excluded)
            + ". This forecast adds "
            + planningCurrency.Trim().ToUpperInvariant()
            + " only.";
    }

    #endregion
}
