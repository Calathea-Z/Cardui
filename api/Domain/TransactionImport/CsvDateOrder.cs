namespace Cardui.Api.Domain.TransactionImport;

/// <summary>
/// Which number is the month when a numeric date is ambiguous.
/// A number above 12 is always the day.
/// </summary>
public enum CsvDateOrder
{
    MonthFirst,
    DayFirst
}
