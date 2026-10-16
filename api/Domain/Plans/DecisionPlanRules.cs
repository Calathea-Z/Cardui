using Cardui.Api.Domain.Recovery;

namespace Cardui.Api.Domain.Plans;

/// <summary>
/// Ranks the three actions in a decision plan and records the assumptions, risks, gaps, milestones, and review conditions around them.
/// The same input always returns the same plan for <see cref="CalculationVersion"/>. Cardui does not move money.
/// </summary>
public static class DecisionPlanRules
{
    /// <summary>
    /// The rule set stored with a plan. Raise it when the ranking or the meaning of a stored field changes.
    /// </summary>
    public const string CalculationVersion = "1";

    /// <summary>
    /// Builds the decision plan from one recovery result.
    /// At most three actions are kept. A shortfall, an unaffordable extra, and missing facts outrank the next debt payment.
    /// Milestones include only dates and amounts the calculation produced.
    /// </summary>
    public static DecisionPlan Compose(DecisionPlanInput input)
    {
        return new DecisionPlan(
            CalculationVersion,
            input.AsOf,
            input.PlanningCurrency,
            input.Kind,
            input.Facts,
            input.Scenario,
            input.Debts,
            input.Steps,
            input.MissingBalanceNames,
            input.ExcludedCurrencies,
            ChooseActions(input),
            Milestones(input),
            Assumptions(input),
            Risks(input),
            Missing(input),
            Reviews(input));
    }

    /// <summary>
    /// True when both plans are the same document.
    /// A repeated save uses this so it does not create another version.
    /// </summary>
    public static bool Same(DecisionPlan left, DecisionPlan right)
    {
        return DecisionPlanJson.Serialize(left) == DecisionPlanJson.Serialize(right);
    }

    #region Private Methods

    /// <summary>
    /// Keeps the first three actions that apply, in rank order.
    /// A household with fewer problems gets fewer actions. No filler action is added.
    /// </summary>
    private static IReadOnlyList<DecisionPlanAction> ChooseActions(DecisionPlanInput input)
    {
        var chosen = new List<DecisionPlanAction>();
        Consider(chosen, CoverShortfall(input));
        Consider(chosen, LowerExtra(input));
        Consider(chosen, AddStartingCash(input));
        Consider(chosen, AddDebt(input));
        Consider(chosen, AddIncome(input));
        Consider(chosen, FinishTerms(input));
        Consider(chosen, RefreshBalances(input));
        Consider(chosen, HoldIncomeLoss(input));
        Consider(chosen, HoldWindfall(input));
        Consider(chosen, PayFirst(input));
        Consider(chosen, UseFreed(input));
        Consider(chosen, ConfirmBills(input));
        Consider(chosen, SetSpending(input));
        Consider(chosen, ChooseFloor(input));
        return chosen;
    }

    /// <summary>
    /// Adds the action at the next rank when the plan still has room and the action applies.
    /// </summary>
    private static void Consider(List<DecisionPlanAction> chosen, DecisionPlanAction? action)
    {
        if (action is null || chosen.Count == 3)
        {
            return;
        }

        chosen.Add(action with { Order = chosen.Count + 1 });
    }

    /// <summary>
    /// The cash shortfall, when dated cash goes below zero.
    /// </summary>
    private static DecisionPlanAction? CoverShortfall(DecisionPlanInput input)
    {
        if (input.Facts.ShortfallOn is not DateOnly on)
        {
            return null;
        }

        return Action(DecisionPlanActionKind.CoverCashShortfall, on: on);
    }

    /// <summary>
    /// The extra payment, when it leaves ordinary spending or a near-term obligation short.
    /// Count distinguishes ordinary spending, a near-term bill or debt payment, or both.
    /// </summary>
    private static DecisionPlanAction? LowerExtra(DecisionPlanInput input)
    {
        var guardrail = input.Facts.Guardrail;
        if (!guardrail.Active)
        {
            return null;
        }

        var count = guardrail.OrdinaryExpenses && guardrail.NearTermObligations
            ? 3
            : guardrail.OrdinaryExpenses
                ? 1
                : guardrail.NearTermObligations
                    ? 2
                    : 3;

        return Action(
            DecisionPlanActionKind.LowerExtra,
            amount: guardrail.Shortfall,
            extra: input.Scenario.MonthlyExtra,
            on: guardrail.On,
            count: count,
            alreadyShortWithoutExtra: guardrail.AlreadyShortWithoutExtra);
    }

    /// <summary>
    /// Starting cash, when no cash account is included.
    /// </summary>
    private static DecisionPlanAction? AddStartingCash(DecisionPlanInput input)
    {
        return input.Facts.StartingCashAccountCount == 0
            ? Action(DecisionPlanActionKind.AddStartingCash)
            : null;
    }

    /// <summary>
    /// A debt to pay, when the household has none.
    /// A debt missing a balance is a term to finish, not a household with no debts.
    /// </summary>
    private static DecisionPlanAction? AddDebt(DecisionPlanInput input)
    {
        return input.Debts.Count == 0 && input.MissingBalanceNames.Count == 0
            ? Action(DecisionPlanActionKind.AddDebt)
            : null;
    }

    /// <summary>
    /// Take-home pay, when none is included.
    /// </summary>
    private static DecisionPlanAction? AddIncome(DecisionPlanInput input)
    {
        return input.Facts.HasIncome
            ? null
            : Action(DecisionPlanActionKind.AddIncome);
    }

    /// <summary>
    /// Missing balances and terms that stop a payoff from being dated.
    /// A payment that does not reduce the balance is included. Reaching the projection limit is not.
    /// </summary>
    private static DecisionPlanAction? FinishTerms(DecisionPlanInput input)
    {
        var gaps = TermGaps(input);
        var count = gaps.Count + input.MissingBalanceNames.Count;
        if (count == 0)
        {
            return null;
        }

        var name = input.MissingBalanceNames.FirstOrDefault() ?? gaps[0].Name;
        return Action(DecisionPlanActionKind.FinishDebtTerms, name: name, count: count);
    }

    /// <summary>
    /// Balances that were not current when the plan was built.
    /// </summary>
    private static DecisionPlanAction? RefreshBalances(DecisionPlanInput input)
    {
        var count = input.Facts.StaleCashCount + input.Facts.StaleDebtCount;
        return count > 0
            ? Action(DecisionPlanActionKind.RefreshBalances, count: count)
            : null;
    }

    /// <summary>
    /// Months of included pay this plan leaves out.
    /// </summary>
    private static DecisionPlanAction? HoldIncomeLoss(DecisionPlanInput input)
    {
        return input.Scenario.IncomeLossMonths > 0
            ? Action(
                DecisionPlanActionKind.HoldIncomeLoss,
                on: input.AsOf,
                count: input.Scenario.IncomeLossMonths)
            : null;
    }

    /// <summary>
    /// A one-time amount that stays in cash and does not pay a debt.
    /// </summary>
    private static DecisionPlanAction? HoldWindfall(DecisionPlanInput input)
    {
        return input.Scenario.WindfallAmount > 0
            ? Action(
                DecisionPlanActionKind.HoldWindfall,
                amount: input.Scenario.WindfallAmount,
                on: input.Scenario.WindfallOn)
            : null;
    }

    /// <summary>
    /// The first debt in the captured payoff order.
    /// </summary>
    private static DecisionPlanAction? PayFirst(DecisionPlanInput input)
    {
        if (input.Debts.Count == 0)
        {
            return null;
        }

        var debt = input.Debts[0];
        return Action(
            DecisionPlanActionKind.PayFirstDebt,
            name: debt.Name,
            amount: debt.Minimum,
            extra: input.Scenario.MonthlyExtra > 0 ? input.Scenario.MonthlyExtra : null,
            on: debt.PaidOffOn);
    }

    /// <summary>
    /// Where the first freed payment goes after that debt is paid off.
    /// Rollover names the next debt when there is one. Freeing the cash does not.
    /// </summary>
    private static DecisionPlanAction? UseFreed(DecisionPlanInput input)
    {
        if (input.Steps.Count == 0)
        {
            return null;
        }

        var step = input.Steps[0];
        return Action(
            DecisionPlanActionKind.UseFreedPayment,
            name: step.Name,
            nextName: NextDebtName(input, step.DebtId),
            amount: step.Minimum,
            extra: step.BreathingRoom,
            on: step.StartsOn);
    }

    /// <summary>
    /// Recurring bills, when none are included.
    /// </summary>
    private static DecisionPlanAction? ConfirmBills(DecisionPlanInput input)
    {
        return input.Facts.HasBills
            ? null
            : Action(DecisionPlanActionKind.ConfirmBills);
    }

    /// <summary>
    /// Flexible monthly spending, when this plan uses none.
    /// </summary>
    private static DecisionPlanAction? SetSpending(DecisionPlanInput input)
    {
        return input.Facts.LivingSpendingMonthly <= 0
            ? Action(DecisionPlanActionKind.SetFlexibleSpending)
            : null;
    }

    /// <summary>
    /// A cash floor, when this plan protects none.
    /// </summary>
    private static DecisionPlanAction? ChooseFloor(DecisionPlanInput input)
    {
        return input.Facts.CashFloor <= 0
            ? Action(DecisionPlanActionKind.ChooseCashToKeep)
            : null;
    }

    /// <summary>
    /// The debt that receives a rolled payment, when this plan rolls payments forward and another debt follows.
    /// </summary>
    private static string? NextDebtName(DecisionPlanInput input, Guid debtId)
    {
        if (input.Kind != PayoffRolloverKind.Rollover)
        {
            return null;
        }

        var index = -1;
        for (var i = 0; i < input.Debts.Count; i++)
        {
            if (input.Debts[i].DebtId == debtId)
            {
                index = i;
                break;
            }
        }

        if (index < 0 || index + 1 >= input.Debts.Count)
        {
            return null;
        }

        return input.Debts[index + 1].Name;
    }

    /// <summary>
    /// Debts whose missing terms or uncovered interest stop the payoff.
    /// </summary>
    private static List<DecisionPlanDebt> TermGaps(DecisionPlanInput input)
    {
        return input.Debts
            .Where(debt => debt.Stop is DebtScheduleStop.RateUnknown
                or DebtScheduleStop.MinimumUnknown
                or DebtScheduleStop.DueDateUnknown
                or DebtScheduleStop.DoesNotPayDown)
            .ToList();
    }

    /// <summary>
    /// Dates and amounts the calculation produced, and no others.
    /// </summary>
    private static IReadOnlyList<DecisionPlanMilestone> Milestones(DecisionPlanInput input)
    {
        var milestones = new List<DecisionPlanMilestone>();
        if (input.Facts.ShortfallOn is DateOnly shortfall)
        {
            milestones.Add(new DecisionPlanMilestone(
                DecisionPlanMilestoneKind.CashShortfall,
                null,
                null,
                shortfall));
        }

        if (input.Facts.RecoveredOn is DateOnly recovered)
        {
            milestones.Add(new DecisionPlanMilestone(
                DecisionPlanMilestoneKind.CashRecovered,
                null,
                null,
                recovered));
        }

        if (input.Facts.ReserveShortfallOn is DateOnly reserve)
        {
            milestones.Add(new DecisionPlanMilestone(
                DecisionPlanMilestoneKind.ReserveShortfall,
                null,
                null,
                reserve));
        }

        if (input.Steps.Count > 0)
        {
            var step = input.Steps[0];
            milestones.Add(new DecisionPlanMilestone(
                DecisionPlanMilestoneKind.FirstPayoff,
                step.Name,
                step.Minimum,
                step.EndedOn));
        }

        if (input.Facts.PaidOffOn is DateOnly paidOff)
        {
            milestones.Add(new DecisionPlanMilestone(
                DecisionPlanMilestoneKind.DebtFree,
                null,
                null,
                paidOff));
        }

        if (input.Facts.RecurringRoom > 0)
        {
            milestones.Add(new DecisionPlanMilestone(
                DecisionPlanMilestoneKind.BreathingRoom,
                null,
                input.Facts.RecurringRoom,
                input.Facts.PaidOffOn));
        }

        return milestones;
    }

    /// <summary>
    /// The calculation rules this plan used, including a cash preview when one was selected.
    /// </summary>
    private static IReadOnlyList<DecisionPlanDetail> Assumptions(DecisionPlanInput input)
    {
        var details = new List<DecisionPlanDetail>
        {
            Detail(DecisionPlanDetailKind.Order, amount: input.Scenario.MonthlyExtra),
            Detail(DecisionPlanDetailKind.Payments),
            Detail(DecisionPlanDetailKind.Interest),
            Detail(DecisionPlanDetailKind.PayoffMonth),
            Detail(DecisionPlanDetailKind.DueDates),
            Detail(DecisionPlanDetailKind.FreedPayment),
            Detail(DecisionPlanDetailKind.CashOutlook),
            Detail(DecisionPlanDetailKind.Spendable),
            Detail(DecisionPlanDetailKind.LowPay),
            Detail(DecisionPlanDetailKind.LeftOut),
            Detail(DecisionPlanDetailKind.Currency),
            Detail(DecisionPlanDetailKind.Limit)
        };

        var scenario = input.Scenario;
        var changed = false;
        if (scenario.IncomeLossMonths > 0)
        {
            changed = true;
            details.Add(Detail(DecisionPlanDetailKind.IncomeLoss, count: scenario.IncomeLossMonths));
        }

        if (scenario.WindfallAmount > 0)
        {
            changed = true;
            details.Add(Detail(
                DecisionPlanDetailKind.Windfall,
                amount: scenario.WindfallAmount,
                on: scenario.WindfallOn));
        }

        if (scenario.LivingSpendingMonthly is decimal spending)
        {
            changed = true;
            details.Add(Detail(DecisionPlanDetailKind.SpendingChange, amount: spending));
        }

        if (scenario.CashFloor is decimal floor)
        {
            changed = true;
            details.Add(Detail(DecisionPlanDetailKind.CashFloorChange, amount: floor));
        }

        if (changed)
        {
            details.Add(Detail(DecisionPlanDetailKind.ScenarioOrder));
        }

        return details;
    }

    /// <summary>
    /// What can make this plan's dates conditional or incomplete.
    /// </summary>
    private static IReadOnlyList<DecisionPlanDetail> Risks(DecisionPlanInput input)
    {
        var details = new List<DecisionPlanDetail>();
        if (input.Facts.ShortfallOn is DateOnly shortfall)
        {
            details.Add(Detail(DecisionPlanDetailKind.CashShortfall, on: shortfall));
        }

        if (input.Facts.ReserveShortfallOn is DateOnly reserve)
        {
            details.Add(Detail(DecisionPlanDetailKind.ReserveShortfall, on: reserve));
        }

        if (input.Facts.Guardrail.Active)
        {
            details.Add(Detail(
                DecisionPlanDetailKind.ExtraGuardrail,
                amount: input.Facts.Guardrail.Shortfall,
                on: input.Facts.Guardrail.On,
                count: input.Facts.Guardrail.AlreadyShortWithoutExtra ? 1 : 0));
        }

        if (PayoffIsConditional(input))
        {
            details.Add(Detail(DecisionPlanDetailKind.PayoffConditional));
        }

        foreach (var debt in input.Debts)
        {
            if (debt.Stop == DebtScheduleStop.DoesNotPayDown)
            {
                details.Add(Detail(DecisionPlanDetailKind.DoesNotPayDown, name: debt.Name));
            }

            if (debt.Stop == DebtScheduleStop.HorizonReached)
            {
                details.Add(Detail(DecisionPlanDetailKind.HorizonReached, name: debt.Name));
            }
        }

        foreach (var currency in input.ExcludedCurrencies)
        {
            details.Add(Detail(DecisionPlanDetailKind.ExcludedCurrency, name: currency));
        }

        var stale = input.Facts.StaleCashCount + input.Facts.StaleDebtCount;
        if (stale > 0)
        {
            details.Add(Detail(DecisionPlanDetailKind.StaleBalance, count: stale));
        }

        if (input.Scenario.IncomeLossMonths > 0)
        {
            details.Add(Detail(
                DecisionPlanDetailKind.IncomeLossRisk,
                count: input.Scenario.IncomeLossMonths));
        }

        if (input.Scenario.WindfallAmount > 0)
        {
            details.Add(Detail(
                DecisionPlanDetailKind.WindfallNotApplied,
                amount: input.Scenario.WindfallAmount,
                on: input.Scenario.WindfallOn));
        }

        foreach (var uncertainty in input.Uncertainties)
        {
            if (uncertainty.Kind == SpendableUncertaintyKind.NoReliableIncome)
            {
                Add(details, Detail(DecisionPlanDetailKind.NoReliableIncome));
            }

            if (uncertainty.Kind == SpendableUncertaintyKind.WindfallLeftOut)
            {
                Add(details, Detail(DecisionPlanDetailKind.WindfallLeftOut, name: NameOrNull(uncertainty.Name)));
            }

            if (uncertainty.Kind == SpendableUncertaintyKind.ExcludedCurrency)
            {
                Add(details, Detail(
                    DecisionPlanDetailKind.ExcludedCurrency,
                    name: NameOrNull(uncertainty.Name)));
            }
        }

        return details;
    }

    /// <summary>
    /// Facts this plan does not have. A blank is missing, not a stored zero.
    /// </summary>
    private static IReadOnlyList<DecisionPlanDetail> Missing(DecisionPlanInput input)
    {
        var details = new List<DecisionPlanDetail>();
        foreach (var name in input.MissingBalanceNames)
        {
            details.Add(Detail(DecisionPlanDetailKind.MissingBalance, name: name));
        }

        foreach (var debt in input.Debts)
        {
            if (debt.Stop == DebtScheduleStop.RateUnknown)
            {
                details.Add(Detail(DecisionPlanDetailKind.UnknownRate, name: debt.Name));
            }

            if (debt.Stop == DebtScheduleStop.MinimumUnknown)
            {
                details.Add(Detail(DecisionPlanDetailKind.UnknownMinimum, name: debt.Name));
            }

            if (debt.Stop == DebtScheduleStop.DueDateUnknown)
            {
                details.Add(Detail(DecisionPlanDetailKind.UnknownDueDate, name: debt.Name));
            }

            if (debt.Stop == DebtScheduleStop.DoesNotPayDown)
            {
                details.Add(Detail(DecisionPlanDetailKind.DoesNotPayDown, name: debt.Name));
            }
        }

        if (!input.Facts.HasIncome)
        {
            details.Add(Detail(DecisionPlanDetailKind.NoIncome));
        }

        if (!input.Facts.HasBills)
        {
            details.Add(Detail(DecisionPlanDetailKind.NoBills));
        }

        if (input.Facts.LivingSpendingMonthly <= 0)
        {
            details.Add(Detail(DecisionPlanDetailKind.NoFlexibleSpending));
        }

        if (input.Facts.CashFloor <= 0)
        {
            details.Add(Detail(DecisionPlanDetailKind.NoCashFloor));
        }

        if (input.Facts.PaymentReviewCount > 0)
        {
            details.Add(Detail(
                DecisionPlanDetailKind.PaymentReview,
                count: input.Facts.PaymentReviewCount));
        }

        foreach (var uncertainty in input.Uncertainties)
        {
            var kind = GapKind(uncertainty.Kind);
            if (kind is DecisionPlanDetailKind gap)
            {
                Add(details, Detail(gap, name: NameOrNull(uncertainty.Name)));
            }
        }

        return details;
    }

    /// <summary>
    /// When this version should be read again. Saving a later edit does not change this version.
    /// </summary>
    private static IReadOnlyList<DecisionPlanDetail> Reviews(DecisionPlanInput input)
    {
        var details = new List<DecisionPlanDetail>
        {
            Detail(DecisionPlanDetailKind.FactsChange)
        };

        if (input.Facts.ShortfallOn is DateOnly shortfall)
        {
            details.Add(Detail(DecisionPlanDetailKind.ShortfallDate, on: shortfall));
        }

        if (input.Scenario.IncomeLossMonths > 0)
        {
            details.Add(Detail(
                DecisionPlanDetailKind.IncomeLossEnds,
                on: input.AsOf.AddMonths(input.Scenario.IncomeLossMonths)));
        }

        if (input.Facts.StaleCashCount + input.Facts.StaleDebtCount > 0)
        {
            details.Add(Detail(DecisionPlanDetailKind.RefreshBalances));
        }

        if (input.Facts.PaymentReviewCount > 0)
        {
            details.Add(Detail(DecisionPlanDetailKind.ZeroBalanceMinimum));
        }

        if (input.Scenario.WindfallOn is DateOnly windfallOn && input.Scenario.WindfallAmount > 0)
        {
            details.Add(Detail(DecisionPlanDetailKind.WindfallArrives, on: windfallOn));
        }

        if (!input.Facts.HasEmergencyGoal)
        {
            details.Add(Detail(DecisionPlanDetailKind.EmergencyChoice));
        }

        return details;
    }

    /// <summary>
    /// True when a payoff date should not be read as a promise.
    /// A cash gap, a reserve gap, a missing balance, or a debt that does not pay off makes it conditional.
    /// </summary>
    private static bool PayoffIsConditional(DecisionPlanInput input)
    {
        if (input.Facts.ShortfallOn is not null
            || input.Facts.ReserveShortfallOn is not null
            || input.MissingBalanceNames.Count > 0)
        {
            return true;
        }

        return input.Debts.Any(debt => debt.Stop != DebtScheduleStop.PaidOff);
    }

    /// <summary>
    /// Maps a spendable gap onto missing information.
    /// Currency, windfall, and unreliable pay are risks instead, so they are not repeated here.
    /// </summary>
    private static DecisionPlanDetailKind? GapKind(SpendableUncertaintyKind kind)
    {
        return kind switch
        {
            SpendableUncertaintyKind.VariableIncome => DecisionPlanDetailKind.VariableIncomeLeftOut,
            SpendableUncertaintyKind.UncertainIncome => DecisionPlanDetailKind.UncertainIncomeLeftOut,
            SpendableUncertaintyKind.OrdinarySpendingUnset => DecisionPlanDetailKind.NoFlexibleSpending,
            SpendableUncertaintyKind.IncomeLeftOut => DecisionPlanDetailKind.IncomeLeftOut,
            SpendableUncertaintyKind.DebtLeftOut => DecisionPlanDetailKind.DebtLeftOut,
            _ => null
        };
    }

    /// <summary>
    /// Adds a detail when the same kind and name are not already listed.
    /// </summary>
    private static void Add(List<DecisionPlanDetail> details, DecisionPlanDetail detail)
    {
        if (details.Any(item => item.Kind == detail.Kind && item.Name == detail.Name))
        {
            return;
        }

        details.Add(detail);
    }

    /// <summary>
    /// A blank label is stored as no name.
    /// </summary>
    private static string? NameOrNull(string name)
    {
        return string.IsNullOrWhiteSpace(name) ? null : name;
    }

    /// <summary>
    /// One action before it receives its rank.
    /// </summary>
    private static DecisionPlanAction Action(
        DecisionPlanActionKind kind,
        string? name = null,
        string? nextName = null,
        decimal? amount = null,
        decimal? extra = null,
        DateOnly? on = null,
        int? count = null,
        bool alreadyShortWithoutExtra = false)
    {
        return new DecisionPlanAction(
            0,
            kind,
            name,
            nextName,
            amount,
            extra,
            on,
            count,
            alreadyShortWithoutExtra);
    }

    /// <summary>
    /// One detail with only the values that kind uses.
    /// </summary>
    private static DecisionPlanDetail Detail(
        DecisionPlanDetailKind kind,
        string? name = null,
        decimal? amount = null,
        DateOnly? on = null,
        int? count = null)
    {
        return new DecisionPlanDetail(kind, name, amount, on, count);
    }

    #endregion
}
