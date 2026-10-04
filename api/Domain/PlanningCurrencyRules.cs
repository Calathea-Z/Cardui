namespace Cardui.Api.Domain;

public static class PlanningCurrencyRules
{
    public const string DefaultCode = "USD";
    public const int CodeLength = 3;

    public static bool IsIncluded(string? isoCurrencyCode, string planningCurrency)
    {
        if (string.IsNullOrWhiteSpace(isoCurrencyCode))
        {
            return true;
        }

        return string.Equals(
            isoCurrencyCode.Trim(),
            planningCurrency.Trim(),
            StringComparison.OrdinalIgnoreCase);
    }

    public static bool TryNormalize(string? value, out string code)
    {
        code = "";
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length != CodeLength)
        {
            return false;
        }

        foreach (var character in normalized)
        {
            if (character is < 'A' or > 'Z')
            {
                return false;
            }
        }

        code = normalized;
        return true;
    }

    public static IReadOnlyList<string> ExcludedCodes(
        IEnumerable<string?> currencyCodes,
        string planningCurrency)
    {
        return currencyCodes
            .Where(code => !IsIncluded(code, planningCurrency))
            .Select(code => code!.Trim().ToUpperInvariant())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToList();
    }
}
