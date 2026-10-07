using Cardui.Api.Domain.Debts;

namespace Cardui.Api.Domain.Recovery;

internal static class PayoffExplanation
{
    /// <summary>
    /// Says whether moving one debt ahead of avalanche, until that debt is paid off, changes the recommendation.
    /// The cash figure is the net of minimums that end sooner and minimums that end later.
    /// </summary>
    public static string Minimum(
        PayoffCandidate? candidate,
        PayoffOrder avalanche,
        string currency,
        bool applied,
        bool constraintApplied,
        bool otherApplied)
    {
        if (candidate is null)
        {
            return "Removing a minimum does not change the avalanche order.";
        }

        var moved = candidate.Order.Debts[0];
        var baseline = avalanche.Debts.First(debt => debt.DebtId == moved.DebtId);
        var months = PayoffTradeoff.MonthsApart(baseline.PaymentsUntilPaidOff, moved.PaymentsUntilPaidOff);
        var sentence = moved.Name
            + " moves first until it is paid off. "
            + Timing(months)
            + " The net minimum cash is "
            + Money(candidate.TradeoffCash, currency)
            + " and "
            + Interest(candidate.InterestDifference, currency)
            + ".";
        if (applied)
        {
            return sentence + " The recommended order removes that minimum.";
        }

        return sentence + Closing(constraintApplied, otherApplied);
    }

    /// <summary>
    /// Says whether bringing one revolving debt under 90 percent of its limit changes the recommendation.
    /// Shared extra returns to avalanche once that debt is under the line.
    /// </summary>
    public static string Utilization(
        PayoffCandidate? candidate,
        PayoffOrder avalanche,
        string currency,
        bool applied,
        bool constraintApplied,
        bool otherApplied)
    {
        if (candidate is null)
        {
            return NoUtilizationMove(avalanche);
        }

        var moved = candidate.Order.Debts[0];
        var baseline = avalanche.Debts.First(debt => debt.DebtId == moved.DebtId);
        var months = PayoffTradeoff.MonthsApart(
            baseline.PaymentsUntilUnderLimit,
            moved.PaymentsUntilUnderLimit);
        var ratio = moved.Utilization ?? 0;
        var sentence = moved.Name
            + " is at "
            + Percent(ratio)
            + " of its limit. Shared extra goes there until it is under 90 percent, then returns to avalanche. "
            + Timing(months)
            + " The utilization cash is "
            + Money(candidate.TradeoffCash, currency)
            + " and "
            + Interest(candidate.InterestDifference, currency)
            + ".";
        if (applied)
        {
            return sentence + " The recommended order lowers that utilization.";
        }

        return sentence + Closing(constraintApplied, otherApplied);
    }

    /// <summary>
    /// Says whether a pay-first or pay-last selection changes the recommendation.
    /// A constraint is honored even when it costs more interest than avalanche.
    /// </summary>
    public static string Constraint(
        PayoffConstraintBuild build,
        PayoffCandidate? candidate,
        string currency,
        bool applied)
    {
        if (build.Notes.Count == 0)
        {
            return "No payoff constraint is selected.";
        }

        var honored = build.Notes.Where(note => note.Honored).ToList();
        var skipped = build.Notes.Where(note => !note.Honored).Select(note => note.Name).ToList();
        if (honored.Count == 0)
        {
            return "The selected constraint is not applied. These debts cannot take a payment: "
                + string.Join(", ", skipped)
                + ".";
        }

        if (!applied || candidate is null)
        {
            var match = "The selected constraint matches avalanche, so it does not change the recommended order.";
            if (skipped.Count == 0)
            {
                return match;
            }

            return match + " These debts cannot take a payment: " + string.Join(", ", skipped) + ".";
        }

        return Describe(honored)
            + " is selected. The net minimum cash is "
            + Money(candidate.TradeoffCash, currency)
            + " and "
            + Interest(candidate.InterestDifference, currency)
            + ". The recommended order honors that constraint.";
    }

    #region Private Methods

    /// <summary>
    /// The utilization sentence when no debt moves ahead of avalanche.
    /// </summary>
    private static string NoUtilizationMove(PayoffOrder avalanche)
    {
        var high = avalanche.Debts.FirstOrDefault(debt =>
            debt.Utilization is decimal utilization && utilization >= DebtSummary.UtilizationLimitNotice);
        if (high is null)
        {
            return "No revolving debt is at or above 90 percent of its limit. Utilization does not change the recommended order.";
        }

        if (avalanche.DebtIds.Count > 0 && avalanche.DebtIds[0] == high.DebtId)
        {
            return high.Name
                + " is already first in avalanche. Utilization does not change the recommended order.";
        }

        return high.Name
            + " cannot take a payment, so utilization does not change the recommended order.";
    }

    /// <summary>
    /// How many months sooner or later this order is than avalanche.
    /// </summary>
    private static string Timing(int months)
    {
        if (months == 0)
        {
            return "That timing is unchanged.";
        }

        var word = Math.Abs(months) == 1 ? "month" : "months";
        return months > 0
            ? "That is " + months + " " + word + " sooner."
            : "That is " + -months + " " + word + " later.";
    }

    /// <summary>
    /// Why this reason is not the recommended order.
    /// </summary>
    private static string Closing(bool constraintApplied, bool otherApplied)
    {
        if (constraintApplied)
        {
            return " The recommended order honors the selected constraint instead.";
        }

        if (otherApplied)
        {
            return " The recommended order uses the larger tradeoff instead.";
        }

        return " Avalanche stays the recommended order.";
    }

    /// <summary>
    /// Names the honored constraints in the order they were read.
    /// </summary>
    private static string Describe(IReadOnlyList<PayoffConstraintNote> honored)
    {
        var parts = honored.Select(note => note.Kind == PayoffConstraintKind.PayFirst
            ? "pay " + note.Name + " first"
            : "pay " + note.Name + " last");
        var text = string.Join(" and ", parts);
        return char.ToUpperInvariant(text[0]) + text[1..];
    }

    /// <summary>
    /// Cents and the planning currency, for an explanation.
    /// </summary>
    private static string Money(decimal amount, string currency)
    {
        return decimal.Round(amount, 2, MidpointRounding.AwayFromZero).ToString("0.00")
            + " "
            + currency.Trim().ToUpperInvariant();
    }

    /// <summary>
    /// States the interest gap against avalanche.
    /// </summary>
    private static string Interest(decimal difference, string currency)
    {
        if (difference == 0)
        {
            return "interest is the same";
        }

        var amount = Money(difference < 0 ? -difference : difference, currency);
        return difference > 0 ? "interest is " + amount + " higher" : "interest is " + amount + " lower";
    }

    /// <summary>
    /// A utilization ratio as a percent with two decimals.
    /// </summary>
    private static string Percent(decimal ratio)
    {
        return decimal.Round(ratio * 100m, 2, MidpointRounding.AwayFromZero).ToString("0.00") + " percent";
    }

    #endregion
}
