using Cardui.Api.Domain.Accounts;
using Cardui.Api.Models;

namespace Cardui.Api.Domain.Recovery;

public static class DebtMinimum
{
    /// <summary>
    /// The monthly amount to pay before extra.
    /// A stored minimum is used as given, including a known zero. A blank minimum on an installment
    /// with a balance, a rate, and a remaining term becomes the level payment for that term.
    /// A revolving debt with a blank minimum stays unknown. The result is not capped at the payoff.
    /// </summary>
    public static decimal? Resolve(
        DebtKind kind,
        decimal balance,
        decimal ratePercent,
        decimal? statedMinimum,
        int? remainingTermMonths)
    {
        if (statedMinimum is decimal stated)
        {
            if (stated < 0)
            {
                return null;
            }

            return AccountLedger.Round(stated);
        }

        if (kind != DebtKind.Installment
            || remainingTermMonths is not int months
            || months < 1
            || balance <= 0)
        {
            return null;
        }

        return LevelPayment(balance, ratePercent, months);
    }

    #region Private Methods

    /// <summary>
    /// The fixed monthly payment that amortizes a balance over a term.
    /// A zero rate divides the balance by the months. A rounded payment can be a cent short;
    /// the schedule's later period pays the leftover because the payoff is then smaller than this amount.
    /// A rate that cannot be raised to the term stays unknown.
    /// </summary>
    private static decimal? LevelPayment(decimal balance, decimal ratePercent, int months)
    {
        if (ratePercent == 0)
        {
            return AtLeastOneCent(AccountLedger.Round(balance / months));
        }

        var monthlyRate = ratePercent / 100m / 12m;
        decimal growth;
        try
        {
            growth = 1m;
            var factor = 1m + monthlyRate;
            for (var step = 0; step < months; step++)
            {
                growth *= factor;
            }
        }
        catch (OverflowException)
        {
            return null;
        }

        if (growth == 1m)
        {
            return null;
        }

        var raw = balance * monthlyRate * growth / (growth - 1m);
        return AtLeastOneCent(AccountLedger.Round(raw));
    }

    /// <summary>
    /// Keeps a calculated payment at a cent when rounding would store zero.
    /// The payment can be larger than the balance, because it includes that month's interest.
    /// </summary>
    private static decimal AtLeastOneCent(decimal payment)
    {
        return payment <= 0 ? 0.01m : payment;
    }

    #endregion
}
