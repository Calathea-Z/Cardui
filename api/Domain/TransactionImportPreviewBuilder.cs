namespace Cardui.Api.Domain;

public static class TransactionImportPreviewBuilder
{
    /// <summary>
    /// Turns mapped CSV rows into import drafts. A row is ready, a duplicate
    /// of an earlier row or an existing transaction, or an error. Category
    /// text that matches a household or system name is kept. A blank category
    /// uses the same description rules as a bank import.
    /// </summary>
    public static IReadOnlyList<TransactionImportDraft> Build(
        CsvTable table,
        TransactionImportColumnMap map,
        IReadOnlySet<TransactionImportDuplicateKey> existingKeys,
        ImportCategoryCatalog categories,
        DateOnly today,
        DateOnly? openingDate)
    {
        var drafts = new List<TransactionImportDraft>(table.Rows.Count);
        var seen = new HashSet<TransactionImportDuplicateKey>();

        foreach (var row in table.Rows)
        {
            drafts.Add(BuildRow(
                row,
                map,
                existingKeys,
                seen,
                categories,
                today,
                openingDate));
        }

        return drafts;
    }

    #region Private Methods

    /// <summary>
    /// Reads one file line and records its duplicate key when the line can be imported.
    /// </summary>
    private static TransactionImportDraft BuildRow(
        CsvDataRow row,
        TransactionImportColumnMap map,
        IReadOnlySet<TransactionImportDuplicateKey> existingKeys,
        HashSet<TransactionImportDuplicateKey> seen,
        ImportCategoryCatalog categories,
        DateOnly today,
        DateOnly? openingDate)
    {
        var date = ReadDate(row, map, out var dateError);
        var name = ReadName(row, map, out var nameError);
        var amount = ReadAmount(row, map, out var amountError);
        var notes = ReadNotes(row, map, out var notesError);
        var (categoryId, categoryName, categoryFromFile, categoryMessage) = ResolveCategory(
            row,
            map,
            name,
            amount,
            categories);

        var message = dateError ?? nameError ?? amountError ?? notesError;
        if (message is null && date is DateOnly parsedDate)
        {
            if (parsedDate > today)
            {
                message = "The transaction date cannot be in the future.";
            }
            else if (openingDate is DateOnly opening && parsedDate < opening)
            {
                message = "The transaction date cannot be before the account opening date.";
            }
        }

        if (message is not null || date is null || name is null || amount is null)
        {
            return new TransactionImportDraft(
                row.LineNumber,
                date,
                name,
                amount,
                categoryId,
                categoryName,
                categoryFromFile,
                notes,
                TransactionImportRowStatus.Error,
                message ?? "This row could not be read.");
        }

        var key = TransactionImportDuplicateKey.Create(date.Value, amount.Value, name);
        TransactionImportRowStatus status;
        if (existingKeys.Contains(key))
        {
            status = TransactionImportRowStatus.Duplicate;
            message = "This matches a transaction already on the account.";
        }
        else if (!seen.Add(key))
        {
            status = TransactionImportRowStatus.Duplicate;
            message = "This matches an earlier row in the file.";
        }
        else
        {
            status = TransactionImportRowStatus.Ready;
            message = categoryMessage;
        }

        return new TransactionImportDraft(
            row.LineNumber,
            date,
            name,
            amount,
            categoryId,
            categoryName,
            categoryFromFile,
            notes,
            status,
            message);
    }

    /// <summary>
    /// Reads the date cell. A blank or unreadable value returns a message.
    /// </summary>
    private static DateOnly? ReadDate(
        CsvDataRow row,
        TransactionImportColumnMap map,
        out string? error)
    {
        var text = Cell(row, map.DateColumn);
        if (text.Length == 0)
        {
            error = "Enter a date.";
            return null;
        }

        var date = CsvDateParser.Parse(text, map.DateOrder);
        if (date is null)
        {
            error = "The date could not be read.";
            return null;
        }

        error = null;
        return date;
    }

    /// <summary>
    /// Reads the name cell and rejects a blank or oversized value.
    /// </summary>
    private static string? ReadName(
        CsvDataRow row,
        TransactionImportColumnMap map,
        out string? error)
    {
        var text = Cell(row, map.NameColumn);
        if (text.Length == 0)
        {
            error = "Enter a name.";
            return null;
        }

        if (text.Length > TransactionImportLimits.MaxNameLength)
        {
            error = "The name is too long.";
            return null;
        }

        error = null;
        return text;
    }

    /// <summary>
    /// Reads one amount column, or debit and credit columns.
    /// Debit is money out. Credit is money in. A CR or DR marker keeps its own direction.
    /// </summary>
    private static decimal? ReadAmount(
        CsvDataRow row,
        TransactionImportColumnMap map,
        out string? error)
    {
        if (map.AmountColumn is int amountColumn)
        {
            var text = Cell(row, amountColumn);
            if (text.Length == 0)
            {
                error = "Enter an amount.";
                return null;
            }

            var parsed = CsvAmountParser.Parse(text);
            if (parsed is null)
            {
                error = "The amount could not be read.";
                return null;
            }

            error = null;
            var amount = parsed.Value.Amount;
            if (!parsed.Value.ExplicitDirection && map.AmountSign == CsvAmountSign.PositiveIn)
            {
                amount = AccountLedger.Round(-amount);
            }

            return amount;
        }

        return ReadSplitAmount(row, map, out error);
    }

    /// <summary>
    /// Combines a debit cell and a credit cell into one signed amount.
    /// </summary>
    private static decimal? ReadSplitAmount(
        CsvDataRow row,
        TransactionImportColumnMap map,
        out string? error)
    {
        var debitText = Cell(row, map.DebitColumn);
        var creditText = Cell(row, map.CreditColumn);
        var debit = ParseSplitCell(debitText, out var debitError);
        var credit = ParseSplitCell(creditText, out var creditError);
        if (debitError is not null || creditError is not null)
        {
            error = debitError ?? creditError;
            return null;
        }

        if (debit is null && credit is null)
        {
            error = "Enter an amount.";
            return null;
        }

        var debitValue = debit.HasValue ? Math.Abs(debit.Value) : 0m;
        var creditValue = credit.HasValue ? Math.Abs(credit.Value) : 0m;
        if (debitValue != 0m && creditValue != 0m)
        {
            error = "A row can have a debit or a credit, not both.";
            return null;
        }

        error = null;
        return creditValue != 0m ? AccountLedger.Round(-creditValue) : AccountLedger.Round(debitValue);
    }

    /// <summary>
    /// Parses a debit or credit cell. A blank cell is absent. Bad text returns a message.
    /// </summary>
    private static decimal? ParseSplitCell(string text, out string? error)
    {
        if (text.Length == 0)
        {
            error = null;
            return null;
        }

        var parsed = CsvAmountParser.Parse(text);
        if (parsed is null)
        {
            error = "The amount could not be read.";
            return null;
        }

        error = null;
        return parsed.Value.Amount;
    }

    /// <summary>
    /// Reads an optional notes cell.
    /// </summary>
    private static string? ReadNotes(
        CsvDataRow row,
        TransactionImportColumnMap map,
        out string? error)
    {
        var text = Cell(row, map.NotesColumn);
        if (text.Length == 0)
        {
            error = null;
            return null;
        }

        if (text.Length > TransactionImportLimits.MaxNotesLength)
        {
            error = "The notes are too long.";
            return null;
        }

        error = null;
        return text;
    }

    /// <summary>
    /// Matches a category name, or suggests one from the description when the cell is blank.
    /// An unmatched name stays uncategorized.
    /// </summary>
    private static (Guid? Id, string? Name, bool FromFile, string? Message) ResolveCategory(
        CsvDataRow row,
        TransactionImportColumnMap map,
        string? name,
        decimal? amount,
        ImportCategoryCatalog categories)
    {
        var text = Cell(row, map.CategoryColumn);
        if (text.Length > 0)
        {
            if (categories.ByName.TryGetValue(ImportCategoryCatalog.NormalizeName(text), out var match))
            {
                return (match.Id, match.Name, true, null);
            }

            var label = text.Length > 80 ? text[..80] : text;
            return (null, null, false, $"No category named \"{label}\" was found.");
        }

        if (name is null || amount is null)
        {
            return (null, null, false, null);
        }

        var key = TransactionCategoryClassifier.GetCategoryKey(name, name, amount);
        if (!categories.ByKey.TryGetValue(key, out var inferred))
        {
            return (null, null, false, null);
        }

        return (inferred.Id, inferred.Name, false, null);
    }

    /// <summary>
    /// Returns a trimmed cell, or empty text when the column is missing.
    /// </summary>
    private static string Cell(CsvDataRow row, int? column)
    {
        if (column is not int index || index < 0 || index >= row.Cells.Count)
        {
            return "";
        }

        return row.Cells[index].Trim();
    }

    #endregion
}
