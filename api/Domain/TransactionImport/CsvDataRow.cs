namespace Cardui.Api.Domain.TransactionImport;

public sealed record CsvDataRow(int LineNumber, IReadOnlyList<string> Cells);
