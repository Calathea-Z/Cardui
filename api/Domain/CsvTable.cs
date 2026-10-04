namespace Cardui.Api.Domain;

public sealed record CsvTable(
    IReadOnlyList<string> Headers,
    IReadOnlyList<CsvDataRow> Rows);
