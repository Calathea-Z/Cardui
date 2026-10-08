using Cardui.Api.Domain.Accounts;
using Cardui.Api.Domain.Debts;
using Cardui.Api.Models;

namespace Cardui.Api.Domain.Recovery;

public static class PayoffPriority
{
    /// <summary>
    /// Compares avalanche, a recommended order, and the user-selected order.
    /// Avalanche pays the highest rate on the first due date first. The recommendation leaves that order only to remove a minimum,
    /// to bring one revolving debt under 90 percent of its limit, or to honor a pay-first or pay-last constraint.
    /// A minimum or utilization change has to keep more cash than the extra interest it costs. A constraint is honored either way.
    /// The pure avalanche order and the user-selected order are always included. A freed minimum is not rolled onto the next debt.
    /// </summary>
    public static PayoffComparison Compare(PayoffPriorityInput input)
    {
        var currency = input.PlanningCurrency;
        var included = Include(input, currency);
        var extra = input.MonthlyExtra <= 0 ? 0 : AccountLedger.Round(input.MonthlyExtra);
        var avalancheDebts = PayoffOrdering.Avalanche(included.Debts);
        var index = AvalancheIndex(avalancheDebts);
        var avalanche = PayoffProjection.Project(PayoffOrderKind.Avalanche, avalancheDebts, index, extra);
        var user = UserOrder(input.UserOrder, avalancheDebts, avalanche, index, extra);
        var constraint = PayoffOrdering.WithConstraints(avalancheDebts, input.Constraints);
        var constraintOrder = ProjectOrder(constraint.Order, avalancheDebts, avalanche, index, extra);
        var minimum = BestPromotion(avalancheDebts, avalanche, index, extra, utilization: false);
        var utilization = BestPromotion(avalancheDebts, avalanche, index, extra, utilization: true);
        var choice = Choose(avalanche, constraint, constraintOrder, minimum, utilization);
        var constraintCandidate = Candidate(
            PayoffAdjustmentKind.Constraint,
            constraintOrder,
            avalanche);
        return new PayoffComparison(
            currency.Trim(),
            extra,
            input.UserOrder is { Count: > 0 },
            avalanche,
            choice.Order,
            user,
            Adjustments(choice, minimum, utilization, constraint, constraintCandidate, avalanche, currency.Trim()),
            included.Excluded,
            Assumptions(currency, included.Excluded));
    }

    #region Private Methods

    /// <summary>
    /// Keeps debts in the planning currency that still have a balance.
    /// A repeated id is kept once. A blank currency counts. Another currency is listed and left out.
    /// </summary>
    private static (List<PayoffDebt> Debts, IReadOnlyList<string> Excluded) Include(
        PayoffPriorityInput input,
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
    /// The avalanche position of each debt, used when utilization extra returns to that order.
    /// </summary>
    private static Dictionary<Guid, int> AvalancheIndex(IReadOnlyList<PayoffDebt> avalanche)
    {
        var index = new Dictionary<Guid, int>(avalanche.Count);
        for (var place = 0; place < avalanche.Count; place++)
        {
            index.TryAdd(avalanche[place].Terms.DebtId, place);
        }

        return index;
    }

    /// <summary>
    /// Projects the user ranking. An empty or unknown selection reuses avalanche.
    /// </summary>
    private static PayoffOrder UserOrder(
        IReadOnlyList<Guid> userOrder,
        IReadOnlyList<PayoffDebt> avalancheDebts,
        PayoffOrder avalanche,
        IReadOnlyDictionary<Guid, int> index,
        decimal extra)
    {
        var ordered = PayoffOrdering.User(avalancheDebts, userOrder);
        return ProjectOrder(ordered, avalancheDebts, avalanche, index, extra) with
        {
            Kind = PayoffOrderKind.UserSelected
        };
    }

    /// <summary>
    /// Projects an order, reusing avalanche when the sequence is the same so the cents match.
    /// </summary>
    private static PayoffOrder ProjectOrder(
        IReadOnlyList<PayoffDebt> ordered,
        IReadOnlyList<PayoffDebt> avalancheDebts,
        PayoffOrder avalanche,
        IReadOnlyDictionary<Guid, int> index,
        decimal extra)
    {
        if (SameDebts(ordered, avalancheDebts))
        {
            return avalanche;
        }

        return PayoffProjection.Project(PayoffOrderKind.Avalanche, ordered, index, extra);
    }

    /// <summary>
    /// Projects moving each eligible debt to the front and keeps the one with the best tradeoff.
    /// A utilization promotion sends extra to that debt only until it is under 90 percent.
    /// A minimum promotion keeps extra there until the debt is paid off. A debt already first is not moved.
    /// </summary>
    private static PayoffCandidate? BestPromotion(
        IReadOnlyList<PayoffDebt> avalancheDebts,
        PayoffOrder avalanche,
        IReadOnlyDictionary<Guid, int> index,
        decimal extra,
        bool utilization)
    {
        if (avalancheDebts.Count == 0)
        {
            return null;
        }

        var firstId = avalancheDebts[0].Terms.DebtId;
        PayoffCandidate? best = null;
        foreach (var debt in avalancheDebts)
        {
            if (debt.Terms.DebtId == firstId || !CanPromote(debt, utilization))
            {
                continue;
            }

            var promoted = PayoffOrdering.Promote(avalancheDebts, debt.Terms.DebtId);
            var projected = PayoffProjection.Project(
                PayoffOrderKind.Recommended,
                promoted,
                index,
                extra,
                utilization ? debt.Terms.DebtId : null);
            var candidate = Candidate(
                utilization ? PayoffAdjustmentKind.Utilization : PayoffAdjustmentKind.MinimumRelease,
                projected,
                avalanche);
            if (best is null || Prefer(candidate, best, utilization))
            {
                best = candidate;
            }
        }

        return best;
    }

    /// <summary>
    /// True when this debt can be calculated, and, for utilization, it is a revolving debt at or above 90 percent.
    /// </summary>
    private static bool CanPromote(PayoffDebt debt, bool utilization)
    {
        var opening = DebtPaymentFacts.Resolve(debt.Terms);
        if (!opening.IsResolved)
        {
            return false;
        }

        if (!utilization)
        {
            return true;
        }

        if (debt.Terms.Kind != DebtKind.Revolving)
        {
            return false;
        }

        var utilizationRatio = DebtRules.Utilization(opening.Balance, debt.CreditLimit);
        return utilizationRatio is decimal value && value >= DebtSummary.UtilizationLimitNotice;
    }

    /// <summary>
    /// True when the candidate should replace the current best.
    /// A larger surplus wins. A tie prefers higher utilization when that is the reason, then the smaller balance,
    /// then the higher rate, then the debt id.
    /// </summary>
    private static bool Prefer(PayoffCandidate candidate, PayoffCandidate current, bool utilization)
    {
        var surplus = candidate.TradeoffCash - candidate.InterestDifference;
        var currentSurplus = current.TradeoffCash - current.InterestDifference;
        if (surplus != currentSurplus)
        {
            return surplus > currentSurplus;
        }

        var left = candidate.Order.Debts[0];
        var right = current.Order.Debts[0];
        if (utilization && left.Utilization != right.Utilization)
        {
            return (left.Utilization ?? 0) > (right.Utilization ?? 0);
        }

        if (left.Balance != right.Balance)
        {
            return left.Balance < right.Balance;
        }

        if (left.Apr != right.Apr)
        {
            return (left.Apr ?? 0) > (right.Apr ?? 0);
        }

        return left.DebtId.CompareTo(right.DebtId) < 0;
    }

    /// <summary>
    /// Scores an order against avalanche.
    /// </summary>
    private static PayoffCandidate Candidate(
        PayoffAdjustmentKind kind,
        PayoffOrder order,
        PayoffOrder avalanche)
    {
        var tradeoff = kind == PayoffAdjustmentKind.Utilization
            ? PayoffTradeoff.UtilizationCash(order, avalanche)
            : PayoffTradeoff.NetMinimumCash(order, avalanche);
        return new PayoffCandidate(
            kind,
            order,
            PayoffTradeoff.InterestDifference(order, avalanche),
            tradeoff);
    }

    /// <summary>
    /// Picks the recommended order. A constraint that changes the sequence wins.
    /// Otherwise the larger positive minimum or utilization tradeoff wins, and a tie prefers utilization.
    /// </summary>
    private static (PayoffOrder Order, PayoffAdjustmentKind? Applied) Choose(
        PayoffOrder avalanche,
        PayoffConstraintBuild constraint,
        PayoffOrder constraintOrder,
        PayoffCandidate? minimum,
        PayoffCandidate? utilization)
    {
        if (constraint.Notes.Any(note => note.Honored) && !SameIds(constraint.Order, avalanche.DebtIds))
        {
            return (constraintOrder with { Kind = PayoffOrderKind.Recommended }, PayoffAdjustmentKind.Constraint);
        }

        var winner = Winner(minimum, utilization);
        if (winner is null || winner.TradeoffCash - winner.InterestDifference <= 0)
        {
            return (avalanche with { Kind = PayoffOrderKind.Recommended }, null);
        }

        return (winner.Order with { Kind = PayoffOrderKind.Recommended }, winner.Kind);
    }

    /// <summary>
    /// The economic adjustment with the larger surplus. An equal surplus prefers utilization.
    /// </summary>
    private static PayoffCandidate? Winner(PayoffCandidate? minimum, PayoffCandidate? utilization)
    {
        if (minimum is null)
        {
            return utilization;
        }

        if (utilization is null)
        {
            return minimum;
        }

        var minimumSurplus = minimum.TradeoffCash - minimum.InterestDifference;
        var utilizationSurplus = utilization.TradeoffCash - utilization.InterestDifference;
        if (utilizationSurplus > minimumSurplus)
        {
            return utilization;
        }

        if (minimumSurplus > utilizationSurplus)
        {
            return minimum;
        }

        return utilization;
    }

    /// <summary>
    /// The three reasons, in minimum, utilization, then constraint order.
    /// </summary>
    private static IReadOnlyList<PayoffAdjustment> Adjustments(
        (PayoffOrder Order, PayoffAdjustmentKind? Applied) choice,
        PayoffCandidate? minimum,
        PayoffCandidate? utilization,
        PayoffConstraintBuild constraint,
        PayoffCandidate constraintCandidate,
        PayoffOrder avalanche,
        string currency)
    {
        var constraintApplied = choice.Applied == PayoffAdjustmentKind.Constraint;
        var minimumApplied = choice.Applied == PayoffAdjustmentKind.MinimumRelease;
        var utilizationApplied = choice.Applied == PayoffAdjustmentKind.Utilization;
        return
        [
            Adjustment(
                PayoffAdjustmentKind.MinimumRelease,
                minimum,
                avalanche,
                minimumApplied,
                PayoffExplanation.Minimum(
                    minimum,
                    avalanche,
                    currency,
                    minimumApplied,
                    constraintApplied,
                    utilizationApplied)),
            Adjustment(
                PayoffAdjustmentKind.Utilization,
                utilization,
                avalanche,
                utilizationApplied,
                PayoffExplanation.Utilization(
                    utilization,
                    avalanche,
                    currency,
                    utilizationApplied,
                    constraintApplied,
                    minimumApplied)),
            Adjustment(
                PayoffAdjustmentKind.Constraint,
                constraint.Notes.Any(note => note.Honored) ? constraintCandidate : null,
                avalanche,
                constraintApplied,
                PayoffExplanation.Constraint(constraint, constraintCandidate, currency, constraintApplied))
        ];
    }

    /// <summary>
    /// One adjustment row. A missing candidate keeps the avalanche order and a zero tradeoff.
    /// </summary>
    private static PayoffAdjustment Adjustment(
        PayoffAdjustmentKind kind,
        PayoffCandidate? candidate,
        PayoffOrder avalanche,
        bool applied,
        string explanation)
    {
        if (candidate is null)
        {
            return new PayoffAdjustment(kind, false, 0, 0, avalanche.DebtIds, explanation);
        }

        return new PayoffAdjustment(
            kind,
            applied,
            candidate.InterestDifference,
            candidate.TradeoffCash,
            candidate.Order.DebtIds,
            explanation);
    }

    /// <summary>
    /// True when both lists carry the same debt ids in the same order.
    /// </summary>
    private static bool SameDebts(IReadOnlyList<PayoffDebt> left, IReadOnlyList<PayoffDebt> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        for (var index = 0; index < left.Count; index++)
        {
            if (left[index].Terms.DebtId != right[index].Terms.DebtId)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// True when a debt list matches an id list in the same order.
    /// </summary>
    private static bool SameIds(IReadOnlyList<PayoffDebt> left, IReadOnlyList<Guid> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        for (var index = 0; index < left.Count; index++)
        {
            if (left[index].Terms.DebtId != right[index])
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// The ranking and interest rules this comparison used.
    /// </summary>
    private static IReadOnlyList<string> Assumptions(string currency, IReadOnlyList<string> excluded)
    {
        var assumptions = new List<string>
        {
            "Avalanche pays the highest rate on the first due date first. The same rate pays the smaller balance first. A debt that cannot be calculated stays at the end.",
            "Interest is one month of simple interest on the balance at the start of the period, rounded to cents away from zero. It is not an average daily balance.",
            "Shared extra goes to the first debt in the order that can take it. Each debt keeps its own extra. Unused extra is not carried to the next month. A freed minimum is not rolled onto another debt.",
            "One round is one payment on each debt, stepped monthly from its own due date. Debts with different due dates still share a round.",
            "Removing a minimum moves one debt ahead of avalanche until that debt is paid off, and only when the net minimum cash is greater than the extra interest.",
            "Lowering utilization moves one revolving debt that is at or above 90 percent of its limit ahead only until it is under 90 percent, and only when that cash is greater than the extra interest. Extra then returns to avalanche. 90 percent is the debt summary's limit notice.",
            "A pay-first or pay-last constraint is honored ahead of those comparisons, even when it costs more interest. The first constraint on a debt is the one used. Pure avalanche and the user-selected order are always included.",
            "A missing rate, minimum, or due date stays unknown and is not given shared extra. A balance that is already zero is left out. A payment that leaves the balance the same or higher stops that debt, and the higher balance stays visible.",
            "The same inputs produce the same order and the same cents."
        };
        if (excluded.Count > 0)
        {
            assumptions.Add(
                "Amounts in another currency are left out: "
                + string.Join(", ", excluded)
                + ". This comparison ranks "
                + currency.Trim().ToUpperInvariant()
                + " only.");
        }

        return assumptions;
    }

    #endregion
}
