using Cardui.Api.Models;

namespace Cardui.Api.Domain.Obligations;

public static class ObligationRules
{
    public const decimal MaxAmount = 100_000_000m;

    public static readonly DateOnly EarliestDueDate = new(2000, 1, 1);

    public static readonly DateOnly LatestDueDate = new(2100, 12, 31);

    /// <summary>
    /// Checks a proposed bill and returns the values to store.
    /// The amount is one payment. A monthly equivalent is not stored.
    /// An empty account means the bill is not tied to an account.
    /// </summary>
    public static bool TryNormalize(
        string? name,
        decimal amount,
        ObligationCadence? cadence,
        DateOnly nextDueDate,
        Guid? accountId,
        ObligationFlexibility? flexibility,
        out ObligationDraft draft,
        out string error)
    {
        draft = default;
        var trimmed = name?.Trim() ?? "";
        if (trimmed.Length == 0)
        {
            error = "A bill name is required.";
            return false;
        }

        if (trimmed.Length > Obligation.NameMaxLength)
        {
            error = "A bill name must be 80 characters or fewer.";
            return false;
        }

        if (!TryReadAmount(amount, out error))
        {
            return false;
        }

        if (cadence is not ObligationCadence cadenceValue)
        {
            error = "Choose how often this bill is due.";
            return false;
        }

        if (nextDueDate < EarliestDueDate || nextDueDate > LatestDueDate)
        {
            error = "Enter the next due date.";
            return false;
        }

        if (flexibility is not ObligationFlexibility flexibilityValue)
        {
            error = "Choose whether this bill is essential or flexible.";
            return false;
        }

        if (accountId == Guid.Empty)
        {
            accountId = null;
        }

        draft = new ObligationDraft(
            trimmed,
            amount,
            cadenceValue,
            nextDueDate,
            accountId,
            flexibilityValue);
        error = "";
        return true;
    }

    #region Private Methods

    /// <summary>
    /// Accepts one payment in dollars and cents, above zero and at most the stored maximum.
    /// </summary>
    private static bool TryReadAmount(decimal amount, out string error)
    {
        if (amount <= 0)
        {
            error = "Enter the amount for one payment.";
            return false;
        }

        if (amount > MaxAmount)
        {
            error = "That amount is too large.";
            return false;
        }

        if (decimal.Round(amount, 2, MidpointRounding.AwayFromZero) != amount)
        {
            error = "Enter the amount in dollars and cents.";
            return false;
        }

        error = "";
        return true;
    }

    #endregion
}
