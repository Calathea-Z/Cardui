using Cardui.Api.Domain.Accounts;

namespace Cardui.Api.Domain.TransactionImport;

public readonly record struct TransactionImportDuplicateKey(
    DateOnly Date,
    decimal Amount,
    string NormalizedName)
{
    /// <summary>
    /// Builds the key used to spot the same posted transaction.
    /// </summary>
    public static TransactionImportDuplicateKey Create(
        DateOnly date,
        decimal amount,
        string name)
    {
        return new(
            date,
            AccountLedger.Round(amount),
            NormalizeName(name));
    }

    /// <summary>
    /// Folds case and repeated spaces so "Coffee  Shop" matches "coffee shop".
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
