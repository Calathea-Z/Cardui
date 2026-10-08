using System.Globalization;

namespace Cardui.Api.Domain.Recovery;

internal static class PayoffRolloverExplanation
{
    /// <summary>
    /// Says which freed payments roll into the next debt, and when the debts are paid off.
    /// A payment that ends with no later debt still open is not described as rolling.
    /// </summary>
    public static string Rollover(PayoffRolloverPath path, string currency)
    {
        var rolled = Directed(path);
        if (path.CashRolled <= 0 || rolled.Count == 0)
        {
            return "No freed payment rolls into a later debt. " + Ending(path);
        }

        return string.Join(" ", rolled.Select(payment => Describe(payment, currency)))
            + " "
            + Ending(path);
    }

    /// <summary>
    /// Says how much of the freed cash is kept each month, and how that changes interest against rollover.
    /// Keeping nothing matches rollover. Keeping at least as much as reclaiming all is said that way.
    /// </summary>
    public static string Reclaim(
        PayoffRolloverPath path,
        decimal requested,
        decimal interestDifference,
        PayoffRolloverPath reclaimAll,
        string currency)
    {
        if (requested <= 0)
        {
            return "Reclaiming nothing matches rolling the freed cash into the next debt.";
        }

        if (path.CashReclaimed <= 0)
        {
            return "There is no freed cash to reclaim. " + Ending(path);
        }

        var keepsAll = path.CashReclaimed == reclaimAll.CashReclaimed
            && path.TotalInterest == reclaimAll.TotalInterest;
        var lead = keepsAll
            ? "Reclaiming "
                + Money(requested, currency)
                + " each month keeps all of the freed cash, "
                + Money(path.CashReclaimed, currency)
                + ", for savings or spending."
            : "Reclaiming "
                + Money(requested, currency)
                + " each month for savings or spending keeps "
                + Money(path.CashReclaimed, currency)
                + ". The rest rolls into the next debt.";
        return lead + " " + Interest(interestDifference, currency) + " " + Ending(path);
    }

    /// <summary>
    /// Says that every freed dollar stays out of the next debt, and how that changes interest against rollover.
    /// </summary>
    public static string ReclaimAll(PayoffRolloverPath path, decimal interestDifference, string currency)
    {
        if (path.CashReclaimed <= 0)
        {
            return "There is no freed cash to reclaim. " + Ending(path);
        }

        return "Reclaiming all of the freed cash keeps "
            + Money(path.CashReclaimed, currency)
            + " for savings or spending. "
            + Interest(interestDifference, currency)
            + " "
            + Ending(path);
    }

    #region Private Methods

    /// <summary>
    /// Freed payments that ended while another debt was still open afterward.
    /// Cash from the last debt has nowhere to roll.
    /// </summary>
    private static IReadOnlyList<PayoffFreedPayment> Directed(PayoffRolloverPath path)
    {
        return path.FreedPayments
            .Where(payment => path.Debts.Any(debt =>
                debt.DebtId != payment.DebtId
                && (debt.PaidOffOn is null || debt.PaidOffOn > payment.EndedOn)))
            .ToList();
    }

    /// <summary>
    /// One debt's payment ending, and the minimum and planned extra that roll the next month.
    /// </summary>
    private static string Describe(PayoffFreedPayment payment, string currency)
    {
        var cash = payment.Extra > 0
            ? "Its "
                + Money(payment.Minimum, currency)
                + " minimum and "
                + Money(payment.Extra, currency)
                + " extra roll"
            : "Its " + Money(payment.Minimum, currency) + " minimum rolls";
        return payment.Name
            + "'s payment ends on "
            + When(payment.EndedOn)
            + ". "
            + cash
            + " into the next debt starting the following month.";
    }

    /// <summary>
    /// The plan's payoff date, or the fact that a debt is still open.
    /// </summary>
    private static string Ending(PayoffRolloverPath path)
    {
        if (path.PaidOffOn is DateOnly paid)
        {
            return "The debts are paid off on " + When(paid) + ".";
        }

        return "A debt is still open.";
    }

    /// <summary>
    /// The interest gap against rolling every freed dollar into the next debt.
    /// </summary>
    private static string Interest(decimal difference, string currency)
    {
        if (difference == 0)
        {
            return "Interest is the same as rolling all of it.";
        }

        var amount = Money(difference < 0 ? -difference : difference, currency);
        return difference > 0
            ? "Interest is " + amount + " higher than rolling all of it."
            : "Interest is " + amount + " lower than rolling all of it.";
    }

    /// <summary>
    /// Cents and the planning currency, for an explanation.
    /// </summary>
    private static string Money(decimal amount, string currency)
    {
        return decimal.Round(amount, 2, MidpointRounding.AwayFromZero).ToString("0.00", CultureInfo.InvariantCulture)
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
