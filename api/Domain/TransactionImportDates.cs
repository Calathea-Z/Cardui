namespace Cardui.Api.Domain;

public static class TransactionImportDates
{
    /// <summary>
    /// Finds the earliest and latest dates that can be read in the date column.
    /// Returns false when no cell is a date, so the caller can skip a lookup.
    /// </summary>
    public static bool TryGetSpan(
        CsvTable table,
        int dateColumn,
        CsvDateOrder dateOrder,
        out DateOnly min,
        out DateOnly max)
    {
        min = default;
        max = default;
        var found = false;

        foreach (var row in table.Rows)
        {
            if (dateColumn < 0 || dateColumn >= row.Cells.Count)
            {
                continue;
            }

            var date = CsvDateParser.Parse(row.Cells[dateColumn], dateOrder);
            if (date is not DateOnly value)
            {
                continue;
            }

            if (!found || value < min)
            {
                min = value;
            }

            if (!found || value > max)
            {
                max = value;
            }

            found = true;
        }

        return found;
    }
}
