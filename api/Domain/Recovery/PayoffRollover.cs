using Cardui.Api.Domain.Accounts;

namespace Cardui.Api.Domain.Recovery;

public static class PayoffRollover
{
    /// <summary>
    /// Compares rolling freed payments into the next debt with reclaiming some or all of that cash.
    /// Rollover is the default. After a modeled payment brings a balance to zero, that debt's minimum and its planned extra
    /// go to the next debt that can take them, starting the following month. The payoff month still pays the debt that is ending.
    /// Reclaim keeps the requested amount of that freed cash each month for savings or spending, and the rest rolls.
    /// Reclaiming all keeps every freed dollar out of the next debt. Shared extra is not part of the freed payment.
    /// </summary>
    public static PayoffRolloverComparison Compare(PayoffRolloverInput input)
    {
        var currency = input.PlanningCurrency.Trim();
        var included = Include(input, currency);
        var ordered = PayoffOrdering.User(
            PayoffOrdering.Avalanche(included.Debts),
            input.Order);
        return Assemble(
            currency,
            NonNegative(input.MonthlyExtra),
            NonNegative(input.ReclaimAmount),
            input.Order is { Count: > 0 },
            ordered,
            included.Excluded,
            input.AsOf);
    }

    #region Private Methods

    /// <summary>
    /// Keeps debts in the planning currency that still have a balance.
    /// A repeated id is kept once. A blank currency counts. Another currency is listed and left out.
    /// </summary>
    private static (List<PayoffDebt> Debts, IReadOnlyList<string> Excluded) Include(
        PayoffRolloverInput input,
        string currency)
    {
        var excluded = PlanningCurrencyRules.ExcludedCodes(
            input.Debts.Select(debt => debt.Terms.Currency),
            currency);
        var seen = new HashSet<Guid>();
        var debts = new List<PayoffDebt>();
        foreach (var debt in input.Debts)
        {
            if (!seen.Add(debt.Terms.DebtId)
                || !PlanningCurrencyRules.IsIncluded(debt.Terms.Currency, currency)
                || AccountLedger.Round(debt.Terms.Balance) <= 0)
            {
                continue;
            }

            debts.Add(debt);
        }

        return (debts, excluded);
    }

    /// <summary>
    /// Projects rollover, the requested reclaim, and reclaiming all, then writes the interest gap and the explanation.
    /// </summary>
    private static PayoffRolloverComparison Assemble(
        string currency,
        decimal extra,
        decimal reclaim,
        bool orderProvided,
        IReadOnlyList<PayoffDebt> ordered,
        IReadOnlyList<string> excluded,
        DateOnly asOf)
    {
        var rollover = PayoffRolloverProjection.Project(
            PayoffRolloverKind.Rollover,
            ordered,
            extra,
            0,
            asOf);
        var partial = PayoffRolloverProjection.Project(
            PayoffRolloverKind.Reclaim,
            ordered,
            extra,
            reclaim,
            asOf);
        var all = PayoffRolloverProjection.Project(
            PayoffRolloverKind.ReclaimAll,
            ordered,
            extra,
            null,
            asOf);
        var partialGap = partial.TotalInterest - rollover.TotalInterest;
        var allGap = all.TotalInterest - rollover.TotalInterest;
        return new PayoffRolloverComparison(
            currency,
            extra,
            reclaim,
            orderProvided,
            rollover with
            {
                Explanation = PayoffRolloverExplanation.Rollover(rollover, currency)
            },
            partial with
            {
                InterestDifference = partialGap,
                Explanation = PayoffRolloverExplanation.Reclaim(partial, reclaim, partialGap, all, currency)
            },
            all with
            {
                InterestDifference = allGap,
                Explanation = PayoffRolloverExplanation.ReclaimAll(all, allGap, currency)
            },
            excluded,
            Assumptions(currency, excluded));
    }

    /// <summary>
    /// Zero when the amount is negative, otherwise the amount in cents.
    /// A negative extra or reclaim directs nothing.
    /// </summary>
    private static decimal NonNegative(decimal amount)
    {
        return amount <= 0 ? 0 : AccountLedger.Round(amount);
    }

    /// <summary>
    /// The rollover and interest rules this comparison used.
    /// </summary>
    private static IReadOnlyList<string> Assumptions(string currency, IReadOnlyList<string> excluded)
    {
        var assumptions = new List<string>
        {
            "Rollover is the default. After a payment brings a balance to zero, that debt's minimum and its planned extra roll into the next debt that can take them.",
            "The month that pays the debt off still pays that debt. The freed cash starts the following month.",
            "The amount that rolls is the planned minimum plus the extra that was planned for that debt, including when the last payment was smaller because the balance was smaller.",
            "Shared extra goes to the first debt that can take it, including leftover in the same month. It is not part of the freed payment. An open debt keeps its own extra until its payment ends.",
            "Unused shared extra and unused rolled cash are not carried to the next month.",
            "A debt that does not pay off does not free its payment. A missing rate, minimum, or due date stays unknown and is not given rolled cash. A balance that is already zero is left out.",
            "Reclaiming keeps some of the freed cash each month for savings or spending, and the rest rolls. Reclaiming all keeps every freed dollar out of the next debt. Those totals count only while a debt is still open.",
            "One round is one payment on each debt, stepped monthly from its own due date. A due date before the start is not replayed; the first payment is the first monthly date on or after the start. Debts with different due dates still share a round. Interest is one month of simple interest on the balance at the start of the period, rounded to cents away from zero.",
            "The same inputs produce the same payments and the same cents."
        };
        if (excluded.Count > 0)
        {
            assumptions.Add(
                "Amounts in another currency are left out: "
                + string.Join(", ", excluded)
                + ". This comparison uses "
                + currency.Trim().ToUpperInvariant()
                + " only.");
        }

        return assumptions;
    }

    #endregion
}
