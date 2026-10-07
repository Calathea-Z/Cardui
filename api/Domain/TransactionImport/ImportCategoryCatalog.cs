namespace Cardui.Api.Domain.TransactionImport;

public sealed record ImportCategoryCatalog(
    IReadOnlyDictionary<string, ImportCategoryMatch> ByName,
    IReadOnlyDictionary<string, ImportCategoryMatch> ByKey)
{
    public static ImportCategoryCatalog Empty { get; } = new(
        new Dictionary<string, ImportCategoryMatch>(),
        new Dictionary<string, ImportCategoryMatch>());

    /// <summary>
    /// Folds case and repeated spaces so a CSV category name can match a stored name.
    /// </summary>
    public static string NormalizeName(string name)
    {
        return string.Join(
            ' ',
            name.Trim()
                .ToLowerInvariant()
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
}
