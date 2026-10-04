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

    /// <summary>
    /// Today's date in the household time zone.
    /// </summary>
    public static DateOnly Today(TimeProvider timeProvider, string timeZoneId)
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        var local = TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), timeZone);
        return DateOnly.FromDateTime(local.DateTime);
    }
}
