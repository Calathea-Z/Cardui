namespace Cardui.Api.Domain;

public static class TransactionImportColumnMapRules
{
    /// <summary>
    /// Checks that the mapped columns exist and that amount is mapped once.
    /// Returns a message when the map cannot be used.
    /// </summary>
    public static string? Validate(TransactionImportColumnMap map, int columnCount)
    {
        if (columnCount < 1)
        {
            return "The CSV needs a header row.";
        }

        if (!IsColumn(map.DateColumn, columnCount))
        {
            return "Choose a date column.";
        }

        if (!IsColumn(map.NameColumn, columnCount))
        {
            return "Choose a name column.";
        }

        if (map.AmountColumn is int amountColumn && !IsColumn(amountColumn, columnCount))
        {
            return "Choose an amount column.";
        }

        if (map.DebitColumn is int debitColumn && !IsColumn(debitColumn, columnCount))
        {
            return "Choose a debit column.";
        }

        if (map.CreditColumn is int creditColumn && !IsColumn(creditColumn, columnCount))
        {
            return "Choose a credit column.";
        }

        if (map.CategoryColumn is int categoryColumn && !IsColumn(categoryColumn, columnCount))
        {
            return "Choose a category column.";
        }

        if (map.NotesColumn is int notesColumn && !IsColumn(notesColumn, columnCount))
        {
            return "Choose a notes column.";
        }

        var hasAmount = map.AmountColumn is not null;
        var hasSplit = map.DebitColumn is not null || map.CreditColumn is not null;
        if (!hasAmount && !hasSplit)
        {
            return "Choose an amount column, or debit and credit columns.";
        }

        if (hasAmount && hasSplit)
        {
            return "Use either one amount column or separate debit and credit columns.";
        }

        if (map.AmountSign is not (CsvAmountSign.PositiveOut or CsvAmountSign.PositiveIn))
        {
            return "Choose how positive amounts are signed.";
        }

        if (map.DateOrder is not (CsvDateOrder.MonthFirst or CsvDateOrder.DayFirst))
        {
            return "Choose a date order.";
        }

        var used = new List<int> { map.DateColumn, map.NameColumn };
        AddUsed(used, map.AmountColumn);
        AddUsed(used, map.DebitColumn);
        AddUsed(used, map.CreditColumn);
        AddUsed(used, map.CategoryColumn);
        AddUsed(used, map.NotesColumn);
        if (used.Distinct().Count() != used.Count)
        {
            return "Choose a different column for each field.";
        }

        return null;
    }

    #region Private Methods

    /// <summary>
    /// True when the index points at a header.
    /// </summary>
    private static bool IsColumn(int index, int columnCount) =>
        index >= 0 && index < columnCount;

    /// <summary>
    /// Adds an optional column to the duplicate check.
    /// </summary>
    private static void AddUsed(List<int> used, int? column)
    {
        if (column is int index)
        {
            used.Add(index);
        }
    }

    #endregion
}
