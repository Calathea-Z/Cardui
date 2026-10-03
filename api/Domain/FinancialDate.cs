namespace Cardui.Api.Domain;

public static class FinancialDate
{
    public static DateOnly Today(TimeProvider timeProvider)
    {
        return DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
    }
}
