using Cardui.Api.Models;

namespace Cardui.Api.Domain;

public static class IncomeSourceRules
{
    public const decimal MaxTakeHomeAmount = 100_000_000m;

    public static readonly DateOnly EarliestPaymentDate = new(2000, 1, 1);

    public static readonly DateOnly LatestPaymentDate = new(2100, 12, 31);

    /// <summary>
    /// Checks a proposed income source and returns the values to store.
    /// The typical amount is one take-home payment. Low, strong, and gross pay are optional.
    /// Gross pay is that same payment before deductions, and it is not estimated from net.
    /// A raise is a later typical amount, at least the current typical pay, and does not replace the current one.
    /// The stored amount stays one payment. A monthly equivalent is not stored.
    /// </summary>
    public static bool TryNormalize(
        string? name,
        decimal takeHomeAmount,
        IncomeCadence? cadence,
        DateOnly nextPaymentDate,
        Guid? contributorId,
        IncomeReliability? reliability,
        out IncomeSourceDraft draft,
        out string error,
        decimal? lowTakeHomeAmount = null,
        decimal? strongTakeHomeAmount = null,
        IReadOnlyList<IncomeRaiseDraft>? raises = null,
        decimal? grossPayAmount = null)
    {
        draft = default;
        var trimmed = name?.Trim() ?? "";
        if (trimmed.Length == 0)
        {
            error = "An income source name is required.";
            return false;
        }

        if (trimmed.Length > IncomeSource.NameMaxLength)
        {
            error = "An income source name must be 80 characters or fewer.";
            return false;
        }

        if (!TryReadPayment(takeHomeAmount, "net pay", out error))
        {
            return false;
        }

        if (!TryReadOptionalPayment(lowTakeHomeAmount, "low net pay", out var low, out error))
        {
            return false;
        }

        if (!TryReadOptionalPayment(strongTakeHomeAmount, "strong net pay", out var strong, out error))
        {
            return false;
        }

        if (low is decimal lowAmount && lowAmount > takeHomeAmount)
        {
            error = "Low net pay cannot be higher than the typical amount.";
            return false;
        }

        if (strong is decimal strongAmount && strongAmount < takeHomeAmount)
        {
            error = "Strong net pay cannot be lower than the typical amount.";
            return false;
        }

        if (!TryReadOptionalPayment(grossPayAmount, "gross pay", out var gross, out error))
        {
            return false;
        }

        if (gross is decimal grossAmount && grossAmount < takeHomeAmount)
        {
            error = "Gross pay cannot be lower than the typical net pay.";
            return false;
        }

        if (cadence is not IncomeCadence cadenceValue)
        {
            error = "Choose how often this income is paid.";
            return false;
        }

        if (nextPaymentDate < EarliestPaymentDate || nextPaymentDate > LatestPaymentDate)
        {
            error = "Enter the next payment date.";
            return false;
        }

        if (reliability is not IncomeReliability reliabilityValue)
        {
            error = "Choose how reliable this income is.";
            return false;
        }

        if (!TryReadRaises(takeHomeAmount, nextPaymentDate, raises, out var raiseDrafts, out error))
        {
            return false;
        }

        if (contributorId == Guid.Empty)
        {
            contributorId = null;
        }

        draft = new IncomeSourceDraft(
            trimmed,
            takeHomeAmount,
            low,
            strong,
            gross,
            cadenceValue,
            nextPaymentDate,
            contributorId,
            reliabilityValue,
            raiseDrafts);
        error = "";
        return true;
    }

    #region Private Methods

    /// <summary>
    /// Accepts a net payment in dollars and cents, above zero and at most the stored maximum.
    /// The label is the field name used in the error.
    /// </summary>
    private static bool TryReadPayment(decimal amount, string label, out string error)
    {
        if (amount <= 0)
        {
            error = $"Enter the {label} for one payment.";
            return false;
        }

        if (amount > MaxTakeHomeAmount)
        {
            error = $"That {label} amount is too large.";
            return false;
        }

        if (decimal.Round(amount, 2, MidpointRounding.AwayFromZero) != amount)
        {
            error = $"Enter the {label} in dollars and cents.";
            return false;
        }

        error = "";
        return true;
    }

    /// <summary>
    /// Accepts a blank scenario, or a net payment that passes the same check as typical pay.
    /// A null amount means that scenario is not recorded.
    /// </summary>
    private static bool TryReadOptionalPayment(
        decimal? amount,
        string label,
        out decimal? normalized,
        out string error)
    {
        normalized = null;
        if (amount is not decimal value)
        {
            error = "";
            return true;
        }

        if (!TryReadPayment(value, label, out error))
        {
            return false;
        }

        normalized = value;
        return true;
    }

    /// <summary>
    /// Accepts expected raises on or after the next payment, each on its own date.
    /// A raise amount cannot be lower than the typical pay. The list is ordered by date. An empty list means no raise is expected.
    /// </summary>
    private static bool TryReadRaises(
        decimal takeHomeAmount,
        DateOnly nextPaymentDate,
        IReadOnlyList<IncomeRaiseDraft>? raises,
        out IReadOnlyList<IncomeRaiseDraft> normalized,
        out string error)
    {
        normalized = [];
        if (raises is null || raises.Count == 0)
        {
            error = "";
            return true;
        }

        var seen = new HashSet<DateOnly>();
        var accepted = new List<IncomeRaiseDraft>(raises.Count);
        foreach (var raise in raises)
        {
            if (!TryReadPayment(raise.TakeHomeAmount, "net pay", out error))
            {
                return false;
            }

            if (raise.TakeHomeAmount < takeHomeAmount)
            {
                error = "A raise cannot be lower than the typical net pay.";
                return false;
            }

            if (raise.EffectiveDate < EarliestPaymentDate || raise.EffectiveDate > LatestPaymentDate)
            {
                error = "Enter the raise date.";
                return false;
            }

            if (raise.EffectiveDate < nextPaymentDate)
            {
                error = "Enter a raise date on or after the next payment.";
                return false;
            }

            if (!seen.Add(raise.EffectiveDate))
            {
                error = "Each expected raise needs its own date.";
                return false;
            }

            accepted.Add(new IncomeRaiseDraft(raise.EffectiveDate, raise.TakeHomeAmount));
        }

        accepted.Sort((left, right) => left.EffectiveDate.CompareTo(right.EffectiveDate));
        normalized = accepted;
        error = "";
        return true;
    }

    #endregion
}
