namespace Cardui.Api.Domain;

public static class FinancialDate
{
    public static DateOnly Today(TimeProvider timeProvider)
    {
        return DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
    }

    public static DateOnly Today(TimeProvider timeProvider, string timeZoneId)
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        var local = TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), timeZone);
        return DateOnly.FromDateTime(local.DateTime);
    }
}
