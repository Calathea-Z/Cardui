using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace Cardui.Api.Security;

public static class PlaidAccessTokenCipher
{
    public const string Prefix = "dp:v1:";

    public const string Purpose = "Cardui.Plaid.AccessToken.v1";

    /// <summary>
    /// Encrypts a Plaid access token and marks it with the protection prefix.
    /// </summary>
    public static string Protect(IDataProtector protector, string accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new ArgumentException("A Plaid access token is required.", nameof(accessToken));
        }

        return Prefix + protector.Protect(accessToken);
    }

    /// <summary>
    /// Decrypts a prefixed token. A missing prefix or a bad payload returns false
    /// and leaves the output empty.
    /// </summary>
    public static bool TryUnprotect(
        IDataProtector protector,
        string storedAccessToken,
        out string accessToken)
    {
        accessToken = string.Empty;
        if (string.IsNullOrWhiteSpace(storedAccessToken)
            || !storedAccessToken.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return false;
        }

        try
        {
            accessToken = protector.Unprotect(storedAccessToken[Prefix.Length..]);
            return !string.IsNullOrWhiteSpace(accessToken);
        }
        catch (CryptographicException)
        {
            accessToken = string.Empty;
            return false;
        }
    }

    /// <summary>
    /// Decrypts a prefixed token. Plaintext and unreadable values are rejected.
    /// </summary>
    public static string Unprotect(IDataProtector protector, string storedAccessToken)
    {
        if (!TryUnprotect(protector, storedAccessToken, out var accessToken))
        {
            throw new CryptographicException(
                "The stored Plaid access token is not a protected value.");
        }

        return accessToken;
    }
}
