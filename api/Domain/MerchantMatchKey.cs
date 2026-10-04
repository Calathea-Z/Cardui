namespace Cardui.Api.Domain;

/// <summary>
/// The text used to find a merchant's other transactions.
/// </summary>
public readonly record struct MerchantMatchKey(string DisplayName, bool MatchesMerchantName)
{
    /// <summary>
    /// Prefers a non-blank merchant name. Otherwise uses the transaction name.
    /// </summary>
    public static MerchantMatchKey Create(string name, string? merchantName)
    {
        var merchantKey = merchantName?.Trim();
        var matchesMerchantName = !string.IsNullOrWhiteSpace(merchantKey);
        var displayName = matchesMerchantName
            ? merchantKey!
            : name.Trim();

        return new MerchantMatchKey(displayName, matchesMerchantName);
    }
}
