using System.Globalization;
using Cardui.Api.Domain.Accounts;

namespace Cardui.Api.Domain.Recovery;

internal static class CashFlowRecoveryExplanation
{
    /// <summary>
    /// Says when each payoff removes its minimum, and the recurring breathing room after the last change.
    /// A freed payment that a later debt still takes is described as committed. Shared extra is named only
    /// once every debt is paid off.
    /// </summary>
    public static string Describe(
        decimal? startingObligation,
        IReadOnlyList<CashFlowRecoveryStep> steps,
        IReadOnlyList<PayoffDebtOutcome> debts,
        decimal releasedExtra,
        decimal recurringRoom,
        string currency)
    {
        var sentences = new List<string> { Opening(startingObligation, currency) };
        if (steps.Count == 0)
        {
            sentences.Add("No payoff removes a monthly obligation.");
        }

        foreach (var step in steps)
        {
            sentences.Add(Step(step, currency));
        }

        foreach (var debt in Remaining(debts))
        {
            sentences.Add(StillDue(debt, currency));
        }

        if (releasedExtra > 0)
        {
            sentences.Add(
                "Once every debt is paid off, "
                + Money(releasedExtra, currency)
                + " of extra is no longer sent.");
        }

        sentences.Add("Recurring breathing room is " + Money(recurringRoom, currency) + " a month.");
        return string.Join(" ", sentences);
    }

    #region Private Methods

    /// <summary>
    /// The known minimums before any payoff.
    /// </summary>
    private static string Opening(decimal? startingObligation, string currency)
    {
        return startingObligation is decimal amount
            ? "Monthly minimums start at " + Money(amount, currency) + "."
            : "Monthly minimums are unknown.";
    }

    /// <summary>
    /// One payoff: the date the minimum is removed, and the breathing room from freed payments after it.
    /// </summary>
    private static string Step(CashFlowRecoveryStep step, string currency)
    {
        var text = step.Name
            + "'s payment ends on "
            + When(step.EndedOn)
            + ". "
            + Removed(step, currency);
        if (step.Extra > 0)
        {
            text += " Its " + Money(step.Extra, currency) + " extra stops then too.";
        }

        if (step.BreathingRoomAdded <= 0)
        {
            return text + " The freed " + Money(step.Amount, currency) + " stays committed to a later debt.";
        }

        text += " Breathing room from the freed payments is "
            + Money(step.BreathingRoom, currency)
            + " a month"
            + From(step)
            + ".";
        if (step.BreathingRoomAdded < step.Amount)
        {
            text += " The rest of the freed payment stays committed to a later debt.";
        }

        return text;
    }

    /// <summary>
    /// The date the minimum is no longer paid.
    /// </summary>
    private static string Removed(CashFlowRecoveryStep step, string currency)
    {
        var amount = Money(step.Minimum, currency);
        return step.StartsOn is DateOnly starts
            ? "Its " + amount + " minimum is removed on " + When(starts) + "."
            : "Its " + amount + " minimum is removed on the following due date, which falls outside the dates this projection uses.";
    }

    /// <summary>
    /// The start of the breathing-room phrase, when the removal date is known.
    /// </summary>
    private static string From(CashFlowRecoveryStep step)
    {
        return step.StartsOn is DateOnly starts ? " from " + When(starts) : "";
    }

    /// <summary>
    /// Debts that did not pay off, in name order.
    /// </summary>
    private static IEnumerable<PayoffDebtOutcome> Remaining(IReadOnlyList<PayoffDebtOutcome> debts)
    {
        return debts
            .Where(debt => debt.Stop != DebtScheduleStop.PaidOff)
            .OrderBy(debt => debt.Name, StringComparer.Ordinal)
            .ThenBy(debt => debt.DebtId);
    }

    /// <summary>
    /// A minimum that is still due, or a debt whose minimum cannot be known.
    /// </summary>
    private static string StillDue(PayoffDebtOutcome debt, string currency)
    {
        return debt.Minimum is decimal minimum
            ? debt.Name + "'s " + Money(minimum, currency) + " minimum remains."
            : debt.Name + " is missing a rate, minimum, or due date.";
    }

    /// <summary>
    /// Cents and the planning currency, for an explanation.
    /// </summary>
    private static string Money(decimal amount, string currency)
    {
        return AccountLedger.Round(amount).ToString("0.00", CultureInfo.InvariantCulture)
            + " "
            + currency.Trim().ToUpperInvariant();
    }

    /// <summary>
    /// A due date in a fixed English form, so the sentence does not depend on the machine's language.
    /// </summary>
    private static string When(DateOnly date)
    {
        return date.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture);
    }

    #endregion
}
