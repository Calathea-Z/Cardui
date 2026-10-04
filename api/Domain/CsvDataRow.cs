namespace Cardui.Api.Domain;

public sealed record CsvDataRow(int LineNumber, IReadOnlyList<string> Cells);
