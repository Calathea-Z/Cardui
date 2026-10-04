using System.Globalization;

namespace Cardui.Api.Domain;

public static class TransactionImportRowSelection
{
    /// <summary>
    /// Reads a comma-separated list of file line numbers.
    /// An empty value selects nothing. A non-number returns a message.
    /// </summary>
    public static string? TryParse(
        string? text,
        out IReadOnlyList<int> lineNumbers)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            lineNumbers = [];
            return null;
        }

        if (text.Length > 20_000)
        {
            lineNumbers = [];
            return "Choose fewer rows to import.";
        }

        var selected = new List<int>();
        foreach (var part in text.Split(
                     ',',
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!int.TryParse(part, NumberStyles.None, CultureInfo.InvariantCulture, out var lineNumber)
                || lineNumber < 1)
            {
                lineNumbers = [];
                return "The selected rows could not be read.";
            }

            selected.Add(lineNumber);
        }

        lineNumbers = selected.Distinct().ToList();
        return null;
    }
}
