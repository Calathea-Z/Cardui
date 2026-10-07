using System.Text;

namespace Cardui.Api.Domain.TransactionImport;

public static class CsvTableParser
{
    /// <summary>
    /// Reads a header and the data rows from CSV text.
    /// Blank rows are omitted. A quoted field may contain commas and line breaks.
    /// </summary>
    public static CsvParseResult Parse(string text)
    {
        if (text.Contains('\0'))
        {
            return CsvParseResult.Failed("The file is not a CSV.");
        }

        text = StripPreamble(text);
        var records = ReadRecords(text);
        if (records is null)
        {
            return CsvParseResult.Failed("The CSV has an unmatched quote.");
        }

        if (records.Count == 0)
        {
            return CsvParseResult.Failed("The CSV needs a header row.");
        }

        var headers = records[0].Cells.Select(cell => cell.Trim()).ToList();
        if (headers.Count == 0 || headers.All(string.IsNullOrWhiteSpace))
        {
            return CsvParseResult.Failed("The CSV needs a header row.");
        }

        var rows = records.Skip(1).ToList();
        if (rows.Count > TransactionImportLimits.MaxDataRows)
        {
            return CsvParseResult.Failed(
                $"A CSV import can include {TransactionImportLimits.MaxDataRows} transactions.");
        }

        return CsvParseResult.Succeeded(new CsvTable(headers, rows));
    }

    #region Private Methods

    /// <summary>
    /// Drops a byte-order mark and an Excel sep= directive before the header.
    /// </summary>
    private static string StripPreamble(string text)
    {
        if (text.Length > 0 && text[0] == '\uFEFF')
        {
            text = text[1..];
        }

        if (!text.StartsWith("sep=", StringComparison.OrdinalIgnoreCase))
        {
            return text;
        }

        var newline = text.IndexOfAny(['\r', '\n']);
        if (newline < 0 || newline > 6)
        {
            return text;
        }

        var rest = text[(newline + 1)..];
        if (rest.StartsWith('\n'))
        {
            rest = rest[1..];
        }

        return rest;
    }

    /// <summary>
    /// Splits records on commas and line breaks, keeping quoted commas and breaks.
    /// Returns null when a quote is never closed. Blank records are left out.
    /// </summary>
    private static List<CsvDataRow>? ReadRecords(string text)
    {
        var records = new List<CsvDataRow>();
        var row = new List<string>();
        var field = new StringBuilder();
        var inQuotes = false;
        var line = 1;
        var recordLine = 1;

        for (var index = 0; index < text.Length; index++)
        {
            var current = text[index];
            if (inQuotes)
            {
                if (current == '"')
                {
                    if (index + 1 < text.Length && text[index + 1] == '"')
                    {
                        field.Append('"');
                        index++;
                        continue;
                    }

                    inQuotes = false;
                    continue;
                }

                if (current is '\r' or '\n')
                {
                    if (current == '\r' && index + 1 < text.Length && text[index + 1] == '\n')
                    {
                        index++;
                    }

                    field.Append('\n');
                    line++;
                    continue;
                }

                field.Append(current);
                continue;
            }

            if (current == '"' && field.Length == 0)
            {
                inQuotes = true;
                continue;
            }

            if (current == ',')
            {
                row.Add(field.ToString());
                field.Clear();
                continue;
            }

            if (current is '\r' or '\n')
            {
                if (current == '\r' && index + 1 < text.Length && text[index + 1] == '\n')
                {
                    index++;
                }

                line++;
                FinishRecord(records, row, field, recordLine);
                row = [];
                recordLine = line;
                continue;
            }

            field.Append(current);
        }

        if (inQuotes)
        {
            return null;
        }

        if (field.Length > 0 || row.Count > 0)
        {
            FinishRecord(records, row, field, recordLine);
        }

        return records;
    }

    /// <summary>
    /// Stores one record when any cell has text. A blank line is skipped.
    /// </summary>
    private static void FinishRecord(
        List<CsvDataRow> records,
        List<string> row,
        StringBuilder field,
        int recordLine)
    {
        row.Add(field.ToString());
        field.Clear();
        if (row.All(string.IsNullOrWhiteSpace))
        {
            return;
        }

        records.Add(new CsvDataRow(recordLine, row));
    }

    #endregion
}
