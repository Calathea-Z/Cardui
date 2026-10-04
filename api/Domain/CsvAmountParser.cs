using System.Globalization;

namespace Cardui.Api.Domain;

public static class CsvAmountParser
{
    /// <summary>
    /// Reads a money cell. Parentheses and a leading minus are negative.
    /// A CR suffix is money in, and a DR suffix is money out. Those suffixes
    /// are an explicit direction. Thousands separators may be commas or dots.
    /// </summary>
    public static ParsedCsvAmount? Parse(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.Length == 0)
        {
            return null;
        }

        var explicitDirection = false;
        var negative = false;
        if (TryRemoveSuffix(ref trimmed, "CR"))
        {
            explicitDirection = true;
            negative = true;
        }
        else if (TryRemoveSuffix(ref trimmed, "DR"))
        {
            explicitDirection = true;
        }

        if (trimmed.StartsWith('(') && trimmed.EndsWith(')') && trimmed.Length >= 2)
        {
            negative = true;
            trimmed = trimmed[1..^1].Trim();
        }

        trimmed = trimmed
            .Replace("$", "", StringComparison.Ordinal)
            .Replace("USD", "", StringComparison.OrdinalIgnoreCase)
            .Trim();

        if (trimmed.StartsWith('+'))
        {
            trimmed = trimmed[1..].Trim();
        }

        if (trimmed.StartsWith('-'))
        {
            negative = true;
            trimmed = trimmed[1..].Trim();
        }

        trimmed = NormalizeSeparators(trimmed);
        if (!decimal.TryParse(
                trimmed,
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out var amount))
        {
            return null;
        }

        if (negative)
        {
            amount = -Math.Abs(amount);
        }

        return new ParsedCsvAmount(AccountLedger.Round(amount), explicitDirection);
    }

    #region Private Methods

    /// <summary>
    /// Removes a CR or DR marker at the end of the cell.
    /// A letter immediately before the marker keeps it, so a word is left alone.
    /// </summary>
    private static bool TryRemoveSuffix(ref string text, string suffix)
    {
        if (!text.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var index = text.Length - suffix.Length;
        if (index > 0 && char.IsLetter(text[index - 1]))
        {
            return false;
        }

        text = text[..index].Trim();
        return text.Length > 0;
    }

    /// <summary>
    /// Turns the last separator into a decimal point and drops the thousands separator.
    /// A single comma with three digits after it is a thousands separator.
    /// </summary>
    private static string NormalizeSeparators(string text)
    {
        var comma = text.LastIndexOf(',');
        var dot = text.LastIndexOf('.');
        if (comma >= 0 && dot >= 0)
        {
            return comma > dot
                ? text.Replace(".", "", StringComparison.Ordinal).Replace(',', '.')
                : text.Replace(",", "", StringComparison.Ordinal);
        }

        if (comma < 0)
        {
            return text;
        }

        var groups = text.Split(',');
        if (groups.Length == 2 && groups[1].Length is 1 or 2)
        {
            return text.Replace(',', '.');
        }

        return text.Replace(",", "", StringComparison.Ordinal);
    }

    #endregion
}
