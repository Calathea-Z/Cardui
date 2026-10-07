namespace Cardui.Api.Domain.Debts;

public static class DebtRate
{
    /// <summary>
    /// The rate used on a date.
    /// A promotional APR applies when it is known and its end date is blank, that date, or later.
    /// After that date, the regular APR is used, and a missing regular APR stays unknown.
    /// </summary>
    public static (decimal? Rate, bool Promotional) InEffect(
        decimal? apr,
        decimal? promotionalApr,
        DateOnly? promotionalEndsOn,
        DateOnly on)
    {
        if (promotionalApr is not null
            && (promotionalEndsOn is null || promotionalEndsOn >= on))
        {
            return (promotionalApr, true);
        }

        return (apr, false);
    }
}
