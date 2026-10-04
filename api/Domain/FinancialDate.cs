namespace Cardui.Api.Domain;

public static class FinancialDate
{
    /// <summary>
    /// Today's date in the local time zone of the supplied clock.
    /// </summary>
    public static DateOnly Today(TimeProvider timeProvider)
    {
        return DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
    }
}
