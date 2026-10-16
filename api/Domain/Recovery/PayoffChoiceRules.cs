namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// Turns a payoff-priority comparison into the order a plan can follow, and checks a reclaim request.
/// The four alternatives stay available even when one of them is the recommendation. Nothing is saved.
/// </summary>
public static class PayoffChoiceRules
{
    /// <summary>
    /// Avalanche, then minimum release, utilization, and the user constraint.
    /// Avalanche is applied when none of those reasons became the recommendation.
    /// </summary>
    public static IReadOnlyList<PayoffAlternative> Alternatives(PayoffComparison comparison)
    {
        var avalanche = comparison.Avalanche.DebtIds;
        var applied = comparison.Adjustments.FirstOrDefault(item => item.Applied)?.Kind;
        return
        [
            new PayoffAlternative(
                PayoffPriorityChoice.Avalanche,
                applied is null,
                false,
                0,
                0,
                avalanche),
            From(comparison, PayoffAdjustmentKind.MinimumRelease, PayoffPriorityChoice.MinimumRelease, avalanche),
            From(comparison, PayoffAdjustmentKind.Utilization, PayoffPriorityChoice.Utilization, avalanche),
            From(comparison, PayoffAdjustmentKind.Constraint, PayoffPriorityChoice.Constraint, avalanche)
        ];
    }

    /// <summary>
    /// The debt ids for the alternative the household selected.
    /// An alternative that does not change the order returns the avalanche ids.
    /// </summary>
    public static IReadOnlyList<Guid> Order(PayoffComparison comparison, PayoffPriorityChoice choice)
    {
        return Alternatives(comparison).First(item => item.Choice == choice).DebtIds;
    }

    /// <summary>
    /// One constraint, or none when the debt or the direction was left out.
    /// </summary>
    public static IReadOnlyList<PayoffConstraint> Constraints(
        Guid? debtId,
        PayoffConstraintKind? kind)
    {
        if (debtId is not Guid id || id == Guid.Empty || kind is not PayoffConstraintKind value)
        {
            return [];
        }

        return [new PayoffConstraint(id, value)];
    }

    /// <summary>
    /// The first reason a priority or reclaim amount cannot be calculated, or null when it can.
    /// A constraint needs a household debt and a direction. A partial pair is rejected. A negative amount is rejected.
    /// </summary>
    public static string? Reject(
        PayoffPriorityChoice priority,
        Guid? constraintDebtId,
        PayoffConstraintKind? constraintKind,
        decimal reclaimAmount,
        IReadOnlyCollection<Guid> debtIds)
    {
        if (!Enum.IsDefined(priority))
        {
            return "Choose a payment priority.";
        }

        if (reclaimAmount < 0)
        {
            return "The amount to keep cannot be negative.";
        }

        var hasDebt = constraintDebtId is Guid id && id != Guid.Empty;
        var hasKind = constraintKind is not null;
        if (priority == PayoffPriorityChoice.Constraint && (!hasDebt || !hasKind))
        {
            return "Choose a debt and whether to pay it first or last.";
        }

        if (hasDebt != hasKind)
        {
            return "Choose a debt and whether to pay it first or last.";
        }

        if (hasDebt && !debtIds.Contains(constraintDebtId!.Value))
        {
            return "Choose a debt in this plan.";
        }

        return null;
    }

    /// <summary>
    /// The first reason a saved path cannot use this freed-cash choice, or null when it can.
    /// Keeping some requires an amount above zero. Keeping none or all does not.
    /// </summary>
    public static string? RejectKind(PayoffRolloverKind kind, decimal reclaimAmount)
    {
        if (!Enum.IsDefined(kind))
        {
            return "Choose how much freed cash to keep.";
        }

        if (kind == PayoffRolloverKind.Reclaim && reclaimAmount <= 0)
        {
            return "Enter how much freed cash to keep each month.";
        }

        return null;
    }

    #region Private Methods

    /// <summary>
    /// Copies one adjustment onto the alternative the page compares.
    /// </summary>
    private static PayoffAlternative From(
        PayoffComparison comparison,
        PayoffAdjustmentKind kind,
        PayoffPriorityChoice choice,
        IReadOnlyList<Guid> avalanche)
    {
        var adjustment = comparison.Adjustments.First(item => item.Kind == kind);
        return new PayoffAlternative(
            choice,
            adjustment.Applied,
            !Same(adjustment.DebtIds, avalanche),
            adjustment.InterestDifference,
            adjustment.TradeoffCash,
            adjustment.DebtIds);
    }

    /// <summary>
    /// True when both id lists are the same sequence.
    /// </summary>
    private static bool Same(IReadOnlyList<Guid> left, IReadOnlyList<Guid> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        for (var index = 0; index < left.Count; index++)
        {
            if (left[index] != right[index])
            {
                return false;
            }
        }

        return true;
    }

    #endregion
}
