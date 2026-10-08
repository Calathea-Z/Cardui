using Cardui.Api.Domain.Accounts;
using Cardui.Api.Domain.Recovery;
using Cardui.Api.Models;

namespace Cardui.Api.Domain.Savings;

public static class HouseholdSavings
{
    /// <summary>
    /// Turns stored rows into the reserve the forecast starts from and the later cash events.
    /// Monthly living spending leaves cash on its day. Cash to keep protects the floor and does not leave cash.
    /// A contribution toward a dated goal raises the reserve and does not reduce cash. Another currency is left out of the starting reserve and still listed.
    /// </summary>
    public static SavingsOutlook Project(
        DateOnly today,
        string planningCurrency,
        IReadOnlyList<SavingsGoalSnapshot> goals)
    {
        var reserve = 0m;
        var everyday = 0m;
        var events = new List<CashFlowEvent>();
        foreach (var goal in goals
            .OrderBy(goal => goal.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(goal => goal.Id))
        {
            if (goal.Kind == SavingsGoalKind.Operating)
            {
                if (PlanningCurrencyRules.IsIncluded(goal.Currency, planningCurrency) && goal.MonthlyAmount > 0)
                {
                    everyday = AccountLedger.Round(goal.MonthlyAmount);
                }

                events.AddRange(LivingSpendingSchedule.Schedule(
                    today,
                    goal.ReadyDay,
                    goal.MonthlyAmount,
                    goal.AmountInUse,
                    goal.Id,
                    goal.Name,
                    goal.Currency));
                continue;
            }

            if (goal.Kind == SavingsGoalKind.Floor)
            {
                if (PlanningCurrencyRules.IsIncluded(goal.Currency, planningCurrency) && goal.FloorAmount > 0)
                {
                    reserve = AccountLedger.Round(reserve + goal.FloorAmount);
                }

                continue;
            }

            AddContributions(today, goal, events);
            if (PlanningCurrencyRules.IsIncluded(goal.Currency, planningCurrency))
            {
                reserve = AccountLedger.Round(reserve + goal.AmountInUse);
            }
        }

        return new SavingsOutlook(
            reserve,
            events,
            everyday,
            goals.Any(goal => goal.Kind == SavingsGoalKind.Floor),
            goals.Any(goal => goal.Kind == SavingsGoalKind.Emergency),
            goals.Count(goal => goal.Kind == SavingsGoalKind.Sinking));
    }

    #region Private Methods

    /// <summary>
    /// Adds the amounts that fill one goal's gap.
    /// A goal that is already funded adds nothing. A past date is due on the start date.
    /// </summary>
    private static void AddContributions(
        DateOnly today,
        SavingsGoalSnapshot goal,
        List<CashFlowEvent> events)
    {
        var plan = SavingsTarget.Plan(
            goal.AmountInUse,
            goal.TargetAmount,
            today,
            goal.TargetDate,
            null);
        foreach (var contribution in plan.ToHitDate)
        {
            events.Add(new CashFlowEvent(
                contribution.Date,
                CashFlowKind.Savings,
                goal.Id,
                goal.Name,
                contribution.Amount,
                goal.Currency));
        }
    }

    #endregion
}
