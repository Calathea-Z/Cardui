namespace Cardui.Api.Domain.Recovery;

public static class PayoffOrdering
{
    /// <summary>
    /// Ranks debts by the rate in effect on the first due date, highest first.
    /// The same rate pays the smaller balance first. A debt that cannot be calculated stays at the end.
    /// Ties after the balance use the name, then the debt id, then the input order.
    /// </summary>
    public static IReadOnlyList<PayoffDebt> Avalanche(IReadOnlyList<PayoffDebt> debts)
    {
        return debts
            .Select((debt, index) => (Debt: debt, Index: index, Opening: DebtPaymentFacts.Resolve(debt.Terms)))
            .OrderBy(item => item.Opening.IsResolved ? 0 : 1)
            .ThenByDescending(item => item.Opening.IsResolved ? item.Opening.RatePercent : 0m)
            .ThenBy(item => item.Opening.Balance)
            .ThenBy(item => item.Debt.Terms.Name, StringComparer.Ordinal)
            .ThenBy(item => item.Debt.Terms.DebtId)
            .ThenBy(item => item.Index)
            .Select(item => item.Debt)
            .ToList();
    }

    /// <summary>
    /// Places the listed debts first and keeps the rest in avalanche order.
    /// An empty list returns avalanche. Unknown ids and repeats are ignored.
    /// </summary>
    public static IReadOnlyList<PayoffDebt> User(
        IReadOnlyList<PayoffDebt> avalanche,
        IReadOnlyList<Guid> userOrder)
    {
        if (userOrder.Count == 0)
        {
            return avalanche;
        }

        var byId = ById(avalanche);
        var seen = new HashSet<Guid>();
        var listed = new List<PayoffDebt>();
        foreach (var id in userOrder)
        {
            if (seen.Add(id) && byId.TryGetValue(id, out var debt))
            {
                listed.Add(debt);
            }
        }

        if (listed.Count == 0)
        {
            return avalanche;
        }

        foreach (var debt in avalanche)
        {
            if (seen.Add(debt.Terms.DebtId))
            {
                listed.Add(debt);
            }
        }

        return listed;
    }

    /// <summary>
    /// Moves one debt to the front and leaves the rest in the order given.
    /// The debt stays put when it is already first or is not in the list.
    /// </summary>
    public static IReadOnlyList<PayoffDebt> Promote(IReadOnlyList<PayoffDebt> order, Guid debtId)
    {
        var chosen = new List<PayoffDebt>();
        var rest = new List<PayoffDebt>();
        foreach (var debt in order)
        {
            if (debt.Terms.DebtId == debtId && chosen.Count == 0)
            {
                chosen.Add(debt);
                continue;
            }

            rest.Add(debt);
        }

        chosen.AddRange(rest);
        return chosen;
    }

    /// <summary>
    /// Builds avalanche with pay-first debts ahead of it and pay-last debts behind the ones that can be paid.
    /// A debt that cannot be calculated stays at the end. The first constraint for a debt wins.
    /// </summary>
    internal static PayoffConstraintBuild WithConstraints(
        IReadOnlyList<PayoffDebt> avalanche,
        IReadOnlyList<PayoffConstraint> constraints)
    {
        var byId = ById(avalanche);
        var seen = new HashSet<Guid>();
        var notes = new List<PayoffConstraintNote>();
        var first = new List<PayoffDebt>();
        var last = new List<PayoffDebt>();
        foreach (var constraint in constraints)
        {
            if (!seen.Add(constraint.DebtId))
            {
                continue;
            }

            if (!byId.TryGetValue(constraint.DebtId, out var debt))
            {
                notes.Add(new PayoffConstraintNote("A selected debt", constraint.Kind, false));
                continue;
            }

            if (!DebtPaymentFacts.Resolve(debt.Terms).IsResolved)
            {
                notes.Add(new PayoffConstraintNote(debt.Terms.Name, constraint.Kind, false));
                continue;
            }

            notes.Add(new PayoffConstraintNote(debt.Terms.Name, constraint.Kind, true));
            if (constraint.Kind == PayoffConstraintKind.PayFirst)
            {
                first.Add(debt);
            }
            else
            {
                last.Add(debt);
            }
        }

        var placed = first.Select(debt => debt.Terms.DebtId)
            .Concat(last.Select(debt => debt.Terms.DebtId))
            .ToHashSet();
        var middle = new List<PayoffDebt>();
        var tail = new List<PayoffDebt>();
        foreach (var debt in avalanche)
        {
            if (placed.Contains(debt.Terms.DebtId))
            {
                continue;
            }

            if (DebtPaymentFacts.Resolve(debt.Terms).IsResolved)
            {
                middle.Add(debt);
            }
            else
            {
                tail.Add(debt);
            }
        }

        var order = new List<PayoffDebt>(first.Count + middle.Count + last.Count + tail.Count);
        order.AddRange(first);
        order.AddRange(middle);
        order.AddRange(last);
        order.AddRange(tail);
        return new PayoffConstraintBuild(order, notes);
    }

    #region Private Methods

    /// <summary>
    /// Maps each debt id to the debt. The first copy of an id wins.
    /// </summary>
    private static Dictionary<Guid, PayoffDebt> ById(IReadOnlyList<PayoffDebt> debts)
    {
        var byId = new Dictionary<Guid, PayoffDebt>();
        foreach (var debt in debts)
        {
            byId.TryAdd(debt.Terms.DebtId, debt);
        }

        return byId;
    }

    #endregion
}
