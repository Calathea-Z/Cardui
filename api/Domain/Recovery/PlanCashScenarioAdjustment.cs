using Cardui.Api.Domain.Accounts;

namespace Cardui.Api.Domain.Recovery;

public static class PlanCashScenarioAdjustment
{
    /// <summary>
    /// Applies a temporary cash preview to the household facts the outlook already loaded.
    /// Pay during the income-loss window is marked to be left out. A windfall is one income event.
    /// A living-spending event equal to the saved monthly amount uses the tried amount. A smaller remainder this month stays.
    /// The cash floor replaces only its own part of the starting reserve. Cash on hand does not change.
    /// </summary>
    public static HouseholdCashOutlookInput Apply(
        HouseholdCashOutlookInput input,
        decimal storedLivingSpendingMonthly,
        decimal storedCashFloor,
        PlanCashScenario scenario)
    {
        return input with
        {
            Savings = Spending(input.Savings, storedLivingSpendingMonthly, scenario.LivingSpendingMonthly),
            StartingReserve = Reserve(input.StartingReserve, storedCashFloor, scenario.CashFloor),
            OmitIncomeBefore = scenario.IncomeLossMonths > 0
                ? input.AsOf.AddMonths(scenario.IncomeLossMonths)
                : null,
            Windfall = Windfall(input, scenario)
        };
    }

    #region Private Methods

    /// <summary>
    /// Replaces full monthly flexible-spending events and leaves a smaller remainder and every savings event as they are.
    /// A tried amount of zero removes those full months. Null keeps the saved events.
    /// </summary>
    private static IReadOnlyList<CashFlowEvent> Spending(
        IReadOnlyList<CashFlowEvent> savings,
        decimal storedMonthly,
        decimal? tried)
    {
        if (tried is not decimal amount)
        {
            return savings;
        }

        var monthly = storedMonthly <= 0 ? 0 : AccountLedger.Round(storedMonthly);
        var next = amount <= 0 ? 0 : AccountLedger.Round(amount);
        var events = new List<CashFlowEvent>();
        foreach (var item in savings)
        {
            if (item.Kind != CashFlowKind.LivingSpending || monthly <= 0 || item.Amount != monthly)
            {
                events.Add(item);
                continue;
            }

            if (next > 0)
            {
                events.Add(item with { Amount = next });
            }
        }

        return events;
    }

    /// <summary>
    /// Swaps the saved floor for the tried floor inside the reserve.
    /// Null keeps the reserve. The result is never negative.
    /// </summary>
    private static decimal Reserve(decimal starting, decimal storedFloor, decimal? tried)
    {
        if (tried is not decimal next)
        {
            return starting;
        }

        var floor = storedFloor <= 0 ? 0 : AccountLedger.Round(storedFloor);
        var replacement = next <= 0 ? 0 : AccountLedger.Round(next);
        var reserve = AccountLedger.Round(starting - floor + replacement);
        return reserve < 0 ? 0 : reserve;
    }

    /// <summary>
    /// One cash inflow on the windfall date, in the planning currency.
    /// A missing amount or date adds nothing.
    /// </summary>
    private static CashFlowEvent? Windfall(HouseholdCashOutlookInput input, PlanCashScenario scenario)
    {
        if (scenario.WindfallAmount <= 0 || scenario.WindfallOn is not DateOnly date)
        {
            return null;
        }

        return new CashFlowEvent(
            date,
            CashFlowKind.Income,
            PlanCashScenario.WindfallSourceId,
            "Windfall",
            AccountLedger.Round(scenario.WindfallAmount),
            input.PlanningCurrency);
    }

    #endregion
}
