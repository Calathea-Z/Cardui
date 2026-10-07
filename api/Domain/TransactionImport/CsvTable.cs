namespace Cardui.Api.Domain.TransactionImport;

public sealed record CsvTable(
    IReadOnlyList<string> Headers,
    IReadOnlyList<CsvDataRow> Rows);
