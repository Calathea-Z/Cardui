using System.Globalization;

namespace Cardui.Api.Domain;

public static class CsvDateParser
{
    private static readonly string[] NamedFormats =
    [
        "MMM d, yyyy",
        "MMMM d, yyyy",
        "MMM d yyyy",
        "MMMM d yyyy",
        "d MMM yyyy",
        "d MMMM yyyy"
    ];

    /// <summary>
    /// Reads a transaction date. Year-first values and named months are
    /// unambiguous. A numeric month and day follow the chosen order unless
    /// one number is greater than 12.
    /// </summary>
    public static DateOnly? Parse(string text, string dateOrder)
    {
        var trimmed = text.Trim();
        if (trimmed.Length == 0)
        {
            return null;
        }

        if (TryNamedMonth(trimmed, out var named))
        {
            return named;
        }

        var datePart = DatePortion(trimmed);
        if (TryYearFirst(datePart, out var yearFirst))
        {
            return yearFirst;
        }

        if (TryMonthAndDay(datePart, dateOrder, out var monthAndDay))
        {
            return monthAndDay;
        }

        return null;
    }

    #region Private Methods

    /// <summary>
    /// Keeps the date when a time follows an ISO date.
    /// </summary>
    private static string DatePortion(string text)
    {
        var separator = text.IndexOfAny(['T', ' ']);
        if (separator <= 0)
        {
            return text;
        }

        var head = text[..separator];
        return head.Count(character => character is '-' or '/') == 2 ? head : text;
    }

    /// <summary>
    /// Reads yyyy-MM-dd and yyyy/MM/dd.
    /// </summary>
    private static bool TryYearFirst(string text, out DateOnly date)
    {
        date = default;
        var parts = text.Split(['-', '/']);
        if (parts.Length != 3 || parts[0].Length != 4)
        {
            return false;
        }

        if (!int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var year)
            || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var month)
            || !int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var day))
        {
            return false;
        }

        return TryCreate(year, month, day, out date);
    }

    /// <summary>
    /// Reads a month name such as Oct 4, 2026.
    /// </summary>
    private static bool TryNamedMonth(string text, out DateOnly date)
    {
        return DateOnly.TryParseExact(
            text,
            NamedFormats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out date);
    }

    /// <summary>
    /// Reads M/d/yyyy or d/M/yyyy. A value over 12 is the day.
    /// A two-digit year from 00 through 69 is 2000 through 2069.
    /// </summary>
    private static bool TryMonthAndDay(string text, string dateOrder, out DateOnly date)
    {
        date = default;
        var parts = text.Split(['-', '/']);
        if (parts.Length != 3)
        {
            return false;
        }

        if (!int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var first)
            || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var second)
            || !int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var year))
        {
            return false;
        }

        if (parts[2].Length != 2 && parts[2].Length != 4)
        {
            return false;
        }

        if (year < 100)
        {
            year += year >= 70 ? 1900 : 2000;
        }

        int month;
        int day;
        if (first > 12 && second <= 12)
        {
            day = first;
            month = second;
        }
        else if (second > 12 && first <= 12)
        {
            month = first;
            day = second;
        }
        else if (dateOrder == CsvDateOrder.DayFirst)
        {
            day = first;
            month = second;
        }
        else
        {
            month = first;
            day = second;
        }

        return TryCreate(year, month, day, out date);
    }

    /// <summary>
    /// Builds a date in the years a bank export can reasonably use.
    /// </summary>
    private static bool TryCreate(int year, int month, int day, out DateOnly date)
    {
        date = default;
        if (year is < 1900 or > 2199)
        {
            return false;
        }

        try
        {
            date = new DateOnly(year, month, day);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    #endregion
}
