namespace Cardui.Api.Domain.CategoryTargets;

public static class CategoryTargetRules
{
    public const decimal MaxAmount = 100_000_000m;

    public static readonly DateOnly EarliestMonth = new(2000, 1, 1);

    public static readonly DateOnly LatestMonth = new(2100, 12, 1);

    /// <summary>
    /// Accepts a calendar month from January 2000 through December 2100.
    /// </summary>
    public static bool TryReadMonth(int year, int month, out DateOnly firstDay, out string error)
    {
        firstDay = default;
        if (month is < 1 or > 12)
        {
            error = "Enter a month from January 2000 through December 2100.";
            return false;
        }

        DateOnly day;
        try
        {
            day = new DateOnly(year, month, 1);
        }
        catch (ArgumentOutOfRangeException)
        {
            error = "Enter a month from January 2000 through December 2100.";
            return false;
        }

        if (day < EarliestMonth || day > LatestMonth)
        {
            error = "Enter a month from January 2000 through December 2100.";
            return false;
        }

        firstDay = day;
        error = "";
        return true;
    }

    /// <summary>
    /// Accepts a target in dollars and cents, from zero through the stored maximum.
    /// Zero is a known target. A blank amount is not zero.
    /// </summary>
    public static bool TryReadAmount(decimal amount, out string error)
    {
        if (amount < 0)
        {
            error = "Enter the target as zero or more.";
            return false;
        }

        if (amount > MaxAmount)
        {
            error = "That amount is too large.";
            return false;
        }

        if (decimal.Round(amount, 2, MidpointRounding.AwayFromZero) != amount)
        {
            error = "Enter the target in dollars and cents.";
            return false;
        }

        error = "";
        return true;
    }

    /// <summary>
    /// True when the first month is strictly before the second.
    /// </summary>
    public static bool IsBefore(int year, int month, int otherYear, int otherMonth)
    {
        return year < otherYear || (year == otherYear && month < otherMonth);
    }
}
