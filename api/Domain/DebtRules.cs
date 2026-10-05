using Cardui.Api.Models;

namespace Cardui.Api.Domain;

public static class DebtRules
{
    public const decimal MaxAmount = 100_000_000m;

    public const decimal MaxApr = 999.999m;

    public const int MaxRemainingTermMonths = 600;

    public static readonly DateOnly EarliestDate = new(2000, 1, 1);

    public static readonly DateOnly LatestDate = new(2100, 12, 31);

    /// <summary>
    /// Checks a proposed debt and returns the values to store.
    /// A blank term stays null. A blank is not stored as zero.
    /// A credit limit is kept only for a revolving debt. A remaining term is kept only for an installment debt.
    /// </summary>
    public static bool TryNormalize(
        string? name,
        DebtKind? kind,
        Guid? accountId,
        decimal? balance,
        DateOnly? balanceAsOf,
        decimal? apr,
        decimal? minimumPayment,
        DateOnly? nextDueDate,
        decimal? creditLimit,
        int? remainingTermMonths,
        decimal? promotionalApr,
        DateOnly? promotionalEndsOn,
        out DebtDraft draft,
        out string error)
    {
        draft = default;
        if (!TryReadName(name, out var trimmed, out error))
        {
            return false;
        }

        if (kind is not DebtKind kindValue)
        {
            error = "Choose revolving or installment.";
            return false;
        }

        if (accountId == Guid.Empty)
        {
            accountId = null;
        }

        if (!TryReadDatedBalance(balance, balanceAsOf, out var balanceValue, out var balanceDate, out error))
        {
            return false;
        }

        if (!TryReadOptionalApr(apr, "APR", out var aprValue, out error))
        {
            return false;
        }

        if (!TryReadOptionalMoney(
                minimumPayment,
                allowZero: true,
                "Enter the minimum in dollars and cents, or leave it blank.",
                out var minimumValue,
                out error))
        {
            return false;
        }

        if (!TryReadOptionalDate(nextDueDate, "Enter the due date.", out var dueDate, out error))
        {
            return false;
        }

        if (!TryReadCreditLimit(kindValue, creditLimit, out var limitValue, out error))
        {
            return false;
        }

        if (!TryReadRemainingTerm(kindValue, remainingTermMonths, out var termValue, out error))
        {
            return false;
        }

        if (!TryReadOptionalApr(promotionalApr, "promotional APR", out var promoApr, out error))
        {
            return false;
        }

        if (!TryReadOptionalDate(
                promotionalEndsOn,
                "Enter the date the promotion ends.",
                out var promoEnds,
                out error))
        {
            return false;
        }

        draft = new DebtDraft(
            trimmed,
            kindValue,
            accountId,
            balanceValue,
            balanceDate,
            aprValue,
            minimumValue,
            dueDate,
            limitValue,
            termValue,
            promoApr,
            promoEnds);
        error = "";
        return true;
    }

    /// <summary>
    /// Share of the credit limit in use, as a ratio.
    /// Null when the balance or the credit limit is unknown. A known zero balance is 0, not unknown.
    /// The ratio can be above 1 when the balance is higher than the limit. It is not stored.
    /// </summary>
    public static decimal? Utilization(decimal? balance, decimal? creditLimit)
    {
        if (balance is not decimal balanceValue || creditLimit is not decimal limit || limit <= 0)
        {
            return null;
        }

        return decimal.Round(balanceValue / limit, 4, MidpointRounding.AwayFromZero);
    }

    #region Private Methods

    /// <summary>
    /// Requires a name within the stored length.
    /// </summary>
    private static bool TryReadName(string? name, out string trimmed, out string error)
    {
        trimmed = name?.Trim() ?? "";
        if (trimmed.Length == 0)
        {
            error = "A debt name is required.";
            return false;
        }

        if (trimmed.Length > Debt.NameMaxLength)
        {
            error = "A debt name must be 80 characters or fewer.";
            return false;
        }

        error = "";
        return true;
    }

    /// <summary>
    /// Reads a balance and the date it was true.
    /// Both stay empty when the balance is unknown. One without the other is rejected.
    /// </summary>
    private static bool TryReadDatedBalance(
        decimal? balance,
        DateOnly? balanceAsOf,
        out decimal? amount,
        out DateOnly? asOf,
        out string error)
    {
        amount = null;
        asOf = null;
        if (balance is null && balanceAsOf is null)
        {
            error = "";
            return true;
        }

        if (balance is null)
        {
            error = "Enter the balance, or clear the date.";
            return false;
        }

        if (balanceAsOf is null)
        {
            error = "Enter the date this balance was true.";
            return false;
        }

        if (!TryReadOptionalMoney(
                balance,
                allowZero: true,
                "Enter the balance in dollars and cents, or leave it blank.",
                out amount,
                out error))
        {
            return false;
        }

        return TryReadOptionalDate(balanceAsOf, "Enter the date this balance was true.", out asOf, out error);
    }

    /// <summary>
    /// Reads an optional dollar amount.
    /// Blank stays null. Zero is kept when allowZero is true, because a known zero is not unknown.
    /// </summary>
    private static bool TryReadOptionalMoney(
        decimal? value,
        bool allowZero,
        string invalidMessage,
        out decimal? amount,
        out string error)
    {
        amount = null;
        if (value is null)
        {
            error = "";
            return true;
        }

        if (value < 0 || (!allowZero && value == 0))
        {
            error = invalidMessage;
            return false;
        }

        if (value > MaxAmount)
        {
            error = "That amount is too large.";
            return false;
        }

        if (decimal.Round(value.Value, 2, MidpointRounding.AwayFromZero) != value)
        {
            error = "Enter the amount in dollars and cents.";
            return false;
        }

        amount = value;
        error = "";
        return true;
    }

    /// <summary>
    /// Reads an optional percent.
    /// Blank stays null. Zero is a known 0% rate. More than three decimal places is rejected.
    /// </summary>
    private static bool TryReadOptionalApr(
        decimal? value,
        string label,
        out decimal? apr,
        out string error)
    {
        apr = null;
        if (value is null)
        {
            error = "";
            return true;
        }

        if (value < 0)
        {
            error = $"Enter the {label} as a percent, or leave it blank.";
            return false;
        }

        if (value > MaxApr)
        {
            error = $"That {label} is too large.";
            return false;
        }

        if (decimal.Round(value.Value, 3, MidpointRounding.AwayFromZero) != value)
        {
            error = $"Enter the {label} with up to three decimal places.";
            return false;
        }

        apr = value;
        error = "";
        return true;
    }

    /// <summary>
    /// Reads an optional calendar date inside the supported range.
    /// Blank stays null.
    /// </summary>
    private static bool TryReadOptionalDate(
        DateOnly? value,
        string invalidMessage,
        out DateOnly? date,
        out string error)
    {
        date = null;
        if (value is null)
        {
            error = "";
            return true;
        }

        if (value < EarliestDate || value > LatestDate)
        {
            error = invalidMessage;
            return false;
        }

        date = value;
        error = "";
        return true;
    }

    /// <summary>
    /// Reads a credit limit for a revolving debt.
    /// An installment debt does not keep a limit. A blank limit stays unknown.
    /// </summary>
    private static bool TryReadCreditLimit(
        DebtKind kind,
        decimal? creditLimit,
        out decimal? limit,
        out string error)
    {
        limit = null;
        if (kind != DebtKind.Revolving || creditLimit is null)
        {
            error = "";
            return true;
        }

        return TryReadOptionalMoney(
            creditLimit,
            allowZero: false,
            "Enter the credit limit, or leave it blank.",
            out limit,
            out error);
    }

    /// <summary>
    /// Reads the months left on an installment debt.
    /// A revolving debt does not keep a remaining term. A blank term stays unknown.
    /// </summary>
    private static bool TryReadRemainingTerm(
        DebtKind kind,
        int? remainingTermMonths,
        out int? months,
        out string error)
    {
        months = null;
        if (kind != DebtKind.Installment || remainingTermMonths is null)
        {
            error = "";
            return true;
        }

        if (remainingTermMonths < 1 || remainingTermMonths > MaxRemainingTermMonths)
        {
            error = "Enter the months left, or leave the term blank.";
            return false;
        }

        months = remainingTermMonths;
        error = "";
        return true;
    }

    #endregion
}
