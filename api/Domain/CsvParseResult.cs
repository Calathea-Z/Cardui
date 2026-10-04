namespace Cardui.Api.Domain;

public sealed record CsvParseResult(CsvTable? Table, string? Error)
{
    /// <summary>
    /// Returns a successful parse.
    /// </summary>
    public static CsvParseResult Succeeded(CsvTable table) => new(table, null);

    /// <summary>
    /// Returns a parse that stopped with a message for the user.
    /// </summary>
    public static CsvParseResult Failed(string error) => new(null, error);
}
