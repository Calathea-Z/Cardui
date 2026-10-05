using Cardui.Api.Models;

namespace Cardui.Api.Domain;

public static class IncomeSourceRules
{
    public const decimal MaxTakeHomeAmount = 100_000_000m;

    public static readonly DateOnly EarliestPaymentDate = new(2000, 1, 1);

    public static readonly DateOnly LatestPaymentDate = new(2100, 12, 31);

    /// <summary>
    /// Checks a proposed income source and returns the values to store.
    /// The amount is one take-home payment. A monthly equivalent is not calculated.
    /// </summary>
    public static bool TryNormalize(
        string? name,
        decimal takeHomeAmount,
        IncomeCadence? cadence,
        DateOnly nextPaymentDate,
        Guid? contributorId,
        IncomeReliability? reliability,
        out IncomeSourceDraft draft,
        out string error)
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

        if (takeHomeAmount <= 0)
        {
            error = "Enter the net pay for one payment.";
            return false;
        }

        if (takeHomeAmount > MaxTakeHomeAmount)
        {
            error = "That net pay amount is too large.";
            return false;
        }

        if (decimal.Round(takeHomeAmount, 2, MidpointRounding.AwayFromZero) != takeHomeAmount)
        {
            error = "Enter the net pay in dollars and cents.";
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

        if (contributorId == Guid.Empty)
        {
            contributorId = null;
        }

        draft = new IncomeSourceDraft(
            trimmed,
            takeHomeAmount,
            cadenceValue,
            nextPaymentDate,
            contributorId,
            reliabilityValue);
        error = "";
        return true;
    }
}
