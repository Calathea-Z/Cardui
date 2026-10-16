using Cardui.Api.Domain.Accounts;
using Cardui.Api.Models;

namespace Cardui.Api.Domain.Recovery;

public static class SpendableRules
{
    /// <summary>
    /// Estimates spendable cash from dated bills, debt payments, ordinary spending, reserved funds, and steady income.
    /// The horizon is the cash outlook's first 30 days. A monthly surplus is not the result: the figure is the lowest day.
    /// Variable and uncertain pay are left out. A windfall is left out. Another currency is left out.
    /// An extra payment warns when it leaves ordinary spending, or a bill or debt payment, short inside that horizon.
    /// </summary>
    public static SpendableReport Project(
        HouseholdCashOutlookInput input,
        IReadOnlyList<DebtAmortizationInput> debts,
        PayoffRolloverComparison withExtra,
        PayoffRolloverComparison minimums,
        IReadOnlyList<string> missingDebtNames,
        bool ordinarySpendingKnown)
    {
        var reliable = ReliableInput(input, debts);
        var rollover = Forecast(reliable, minimums.Rollover.Schedules);
        var partial = Forecast(reliable, minimums.Reclaim.Schedules);
        var reclaim = Forecast(reliable, minimums.ReclaimAll.Schedules);
        var through = HorizonEnd(input.AsOf, rollover);
        var reserved = input.StartingReserve <= 0 ? 0 : AccountLedger.Round(input.StartingReserve);
        var uncertainties = Uncertainties(
            input,
            debts,
            missingDebtNames,
            ordinarySpendingKnown,
            through);
        var extra = withExtra.MonthlyExtra;
        return new SpendableReport(
            Estimate(
                rollover,
                extra > 0 ? Forecast(reliable, withExtra.Rollover.Schedules) : rollover,
                extra,
                reserved,
                uncertainties),
            Estimate(
                partial,
                extra > 0 ? Forecast(reliable, withExtra.Reclaim.Schedules) : partial,
                extra,
                reserved,
                uncertainties),
            Estimate(
                reclaim,
                extra > 0 ? Forecast(reliable, withExtra.ReclaimAll.Schedules) : reclaim,
                extra,
                reserved,
                uncertainties));
    }

    #region Private Methods

    /// <summary>
    /// The forecast input that counts only steady pay in the planning currency.
    /// A windfall is omitted here. Income the preview leaves out stays omitted. Bills, debts, and savings stay dated.
    /// </summary>
    private static CashForecastInput ReliableInput(
        HouseholdCashOutlookInput input,
        IReadOnlyList<DebtAmortizationInput> debts)
    {
        return new CashForecastInput(
            input.PlanningCurrency,
            ForecastIncomeBasis.Typical,
            input.AsOf,
            input.StartingCash,
            input.StartingReserve,
            input.Incomes
                .Where(income =>
                    income.Reliability == IncomeReliability.Steady
                    && Included(income.Currency, input.PlanningCurrency))
                .Select(Steady)
                .ToList(),
            input.Bills,
            debts,
            input.Savings,
            input.OmitIncomeBefore,
            null);
    }

    /// <summary>
    /// One steady paycheck at its typical amount, including later raises.
    /// </summary>
    private static DatedIncome Steady(HouseholdIncome income)
    {
        return new DatedIncome(
            income.Id,
            income.Name,
            income.Currency,
            income.TypicalAmount,
            income.Cadence,
            income.NextPaymentDate,
            income.Raises);
    }

    /// <summary>
    /// Walks one set of debt payments. The schedules already include rollover or the extra when the caller built them that way.
    /// </summary>
    private static CashForecastReport Forecast(
        CashForecastInput input,
        IReadOnlyList<DebtSchedule> schedules)
    {
        return CashForecast.Project(input, schedules);
    }

    /// <summary>
    /// The last day of the 30-day walk, or the start when the walk has no days.
    /// </summary>
    private static DateOnly HorizonEnd(DateOnly asOf, CashForecastReport forecast)
    {
        return forecast.Days.Count == 0 ? asOf : forecast.Days[^1].Date;
    }

    /// <summary>
    /// The lowest day on the minimums path, and the guardrail for the path that includes the extra.
    /// </summary>
    private static SpendableEstimate Estimate(
        CashForecastReport minimums,
        CashForecastReport aggressive,
        decimal monthlyExtra,
        decimal reserved,
        IReadOnlyList<SpendableUncertainty> uncertainties)
    {
        if (minimums.Days.Count == 0)
        {
            return Empty(minimums.AsOf, reserved, uncertainties);
        }

        var lowest = LowestAvailable(minimums.Days);
        return new SpendableEstimate(
            minimums.Days[0].Date,
            minimums.Days[^1].Date,
            lowest.Available < 0 ? 0 : lowest.Available,
            lowest.Available,
            lowest.Date,
            Sum(minimums.Days, CashFlowKind.Income),
            Sum(minimums.Days, CashFlowKind.Bill, CashFlowKind.DebtPayment),
            Sum(minimums.Days, CashFlowKind.LivingSpending),
            reserved,
            lowest.Available < 0,
            Guard(monthlyExtra, minimums.Days, aggressive.Days),
            uncertainties);
    }

    /// <summary>
    /// An estimate that cannot be walked. Spendable is zero and the day is the start.
    /// </summary>
    private static SpendableEstimate Empty(
        DateOnly asOf,
        decimal reserved,
        IReadOnlyList<SpendableUncertainty> uncertainties)
    {
        return new SpendableEstimate(
            asOf,
            asOf,
            0,
            0,
            asOf,
            0,
            0,
            0,
            reserved,
            false,
            Inactive(),
            uncertainties);
    }

    /// <summary>
    /// The day with the smallest cash left after the reserve. The earlier day wins a tie.
    /// </summary>
    private static CashDay LowestAvailable(IReadOnlyList<CashDay> days)
    {
        var lowest = days[0];
        for (var index = 1; index < days.Count; index++)
        {
            if (days[index].Available < lowest.Available)
            {
                lowest = days[index];
            }
        }

        return lowest;
    }

    /// <summary>
    /// The sum of one or more event kinds inside the horizon, rounded to cents.
    /// </summary>
    private static decimal Sum(IReadOnlyList<CashDay> days, params CashFlowKind[] kinds)
    {
        var total = 0m;
        foreach (var day in days)
        {
            foreach (var item in day.Events)
            {
                if (kinds.Contains(item.Kind))
                {
                    total += item.Amount;
                }
            }
        }

        return AccountLedger.Round(total);
    }

    /// <summary>
    /// Warns when the extra makes a day with ordinary spending, a bill, or a debt payment short, and that day is worse than minimums.
    /// The first such date is the one reported. No extra leaves the guardrail inactive.
    /// </summary>
    private static SpendableGuardrail Guard(
        decimal monthlyExtra,
        IReadOnlyList<CashDay> minimums,
        IReadOnlyList<CashDay> aggressive)
    {
        if (monthlyExtra <= 0)
        {
            return Inactive();
        }

        var baseline = minimums.ToDictionary(day => day.Date);
        DateOnly? on = null;
        var ordinary = false;
        var obligations = false;
        var already = false;
        var shortfall = 0m;
        foreach (var day in aggressive)
        {
            if (!baseline.TryGetValue(day.Date, out var before) || !Short(day) || !Worse(day, before))
            {
                continue;
            }

            var living = day.Events.Any(item => item.Kind == CashFlowKind.LivingSpending);
            var obligation = day.Events.Any(item =>
                item.Kind is CashFlowKind.Bill or CashFlowKind.DebtPayment);
            if (!living && !obligation)
            {
                continue;
            }

            if (on is null)
            {
                on = day.Date;
                shortfall = Deficit(day);
                already = Short(before);
            }

            ordinary |= living;
            obligations |= obligation;
        }

        if (on is null)
        {
            return Inactive();
        }

        return new SpendableGuardrail(true, ordinary, obligations, on, shortfall, already);
    }

    /// <summary>
    /// An extra that does not leave the horizon short.
    /// </summary>
    private static SpendableGuardrail Inactive()
    {
        return new SpendableGuardrail(false, false, false, null, 0, false);
    }

    /// <summary>
    /// True when cash, or cash after the reserve, is below zero.
    /// </summary>
    private static bool Short(CashDay day)
    {
        return day.Cash < 0 || day.Available < 0;
    }

    /// <summary>
    /// True when the extra path is below the minimums path that day.
    /// </summary>
    private static bool Worse(CashDay day, CashDay before)
    {
        return day.Available < before.Available || day.Cash < before.Cash;
    }

    /// <summary>
    /// How far cash after the reserve is below zero. When only cash itself is negative, that gap is used.
    /// </summary>
    private static decimal Deficit(CashDay day)
    {
        var gap = day.Available < 0 ? -day.Available : -day.Cash;
        return gap <= 0 ? 0 : AccountLedger.Round(gap);
    }

    /// <summary>
    /// The reasons this estimate leaves something out, in a stable order.
    /// A paycheck in another currency is named once, as that currency, and is not also called variable.
    /// </summary>
    private static List<SpendableUncertainty> Uncertainties(
        HouseholdCashOutlookInput input,
        IReadOnlyList<DebtAmortizationInput> debts,
        IReadOnlyList<string> missingDebtNames,
        bool ordinarySpendingKnown,
        DateOnly through)
    {
        var reasons = new List<SpendableUncertainty>();
        var steady = false;
        foreach (var income in input.Incomes.OrderBy(item => item.Name, StringComparer.Ordinal).ThenBy(item => item.Id))
        {
            if (!Included(income.Currency, input.PlanningCurrency))
            {
                continue;
            }

            if (income.Reliability == IncomeReliability.Steady)
            {
                steady = true;
                continue;
            }

            reasons.Add(new SpendableUncertainty(
                income.Reliability == IncomeReliability.Variable
                    ? SpendableUncertaintyKind.VariableIncome
                    : SpendableUncertaintyKind.UncertainIncome,
                income.Name));
        }

        if (!steady)
        {
            reasons.Add(new SpendableUncertainty(SpendableUncertaintyKind.NoReliableIncome, ""));
        }

        if (!ordinarySpendingKnown)
        {
            reasons.Add(new SpendableUncertainty(SpendableUncertaintyKind.OrdinarySpendingUnset, ""));
        }

        if (input.Windfall is CashFlowEvent windfall)
        {
            reasons.Add(new SpendableUncertainty(SpendableUncertaintyKind.WindfallLeftOut, windfall.Name));
        }

        if (input.OmitIncomeBefore is not null)
        {
            reasons.Add(new SpendableUncertainty(SpendableUncertaintyKind.IncomeLeftOut, ""));
        }

        foreach (var code in Excluded(input, debts))
        {
            reasons.Add(new SpendableUncertainty(SpendableUncertaintyKind.ExcludedCurrency, code));
        }

        foreach (var name in missingDebtNames)
        {
            reasons.Add(new SpendableUncertainty(SpendableUncertaintyKind.DebtLeftOut, name));
        }

        foreach (var debt in debts.OrderBy(item => item.Name, StringComparer.Ordinal).ThenBy(item => item.DebtId))
        {
            if (!Included(debt.Currency, input.PlanningCurrency) || PaidInside(debt, input.AsOf, through))
            {
                continue;
            }

            reasons.Add(new SpendableUncertainty(SpendableUncertaintyKind.DebtLeftOut, debt.Name));
        }

        return reasons;
    }

    /// <summary>
    /// True when the debt has a payment inside the horizon, or nothing is owed.
    /// A missing term, or a payment that does not reduce the balance before the horizon, is left out.
    /// </summary>
    private static bool PaidInside(DebtAmortizationInput debt, DateOnly from, DateOnly through)
    {
        var schedule = DebtAmortization.Project(debt, through, from);
        if (schedule.Stop is DebtScheduleStop.PaidOff or DebtScheduleStop.HorizonReached)
        {
            return true;
        }

        return schedule.Periods.Any(period => period.DueDate >= from && period.DueDate <= through);
    }

    /// <summary>
    /// Currency codes on the household facts that do not count in the planning currency.
    /// </summary>
    private static IReadOnlyList<string> Excluded(
        HouseholdCashOutlookInput input,
        IReadOnlyList<DebtAmortizationInput> debts)
    {
        var codes = input.Incomes.Select(income => (string?)income.Currency)
            .Concat(input.Bills.Select(bill => (string?)bill.Currency))
            .Concat(debts.Select(debt => (string?)debt.Currency))
            .Concat(input.Savings.Select(item => (string?)item.Currency));
        if (input.Windfall is CashFlowEvent windfall)
        {
            codes = codes.Append(windfall.Currency);
        }

        return PlanningCurrencyRules.ExcludedCodes(codes, input.PlanningCurrency);
    }

    /// <summary>
    /// True when a row counts in the planning currency. A blank currency counts.
    /// </summary>
    private static bool Included(string? currency, string planningCurrency)
    {
        return PlanningCurrencyRules.IsIncluded(currency, planningCurrency);
    }

    #endregion
}
