using System.Security.Cryptography;
using System.Text;
using Cardui.Api.Exceptions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Cardui.Api.Security;

public static class PlaidWebhookVerifier
{
    private static readonly TimeSpan MaximumAge = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Reads the key id from an ES256 Plaid webhook JWT. Any other algorithm is rejected
    /// before a key is fetched.
    /// </summary>
    public static string ReadKeyId(string jwt)
    {
        var tokenHandler = new JsonWebTokenHandler();
        if (string.IsNullOrWhiteSpace(jwt) || !tokenHandler.CanReadToken(jwt))
        {
            throw new PlaidWebhookRejectedException();
        }

        var token = tokenHandler.ReadJsonWebToken(jwt);
        if (!string.Equals(token.Alg, SecurityAlgorithms.EcdsaSha256, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(token.Kid))
        {
            throw new PlaidWebhookRejectedException();
        }

        return token.Kid;
    }

    /// <summary>
    /// Checks the webhook signature, age, and body hash against the raw request bytes.
    /// </summary>
    public static async Task Verify(
        string jwt,
        ReadOnlyMemory<byte> rawBody,
        JsonWebKey key,
        DateTimeOffset utcNow)
    {
        var token = await ReadSignedToken(jwt, key);
        if (!IssuedAtIsCurrent(token, utcNow) || !BodyHashMatches(token, rawBody.Span))
        {
            throw new PlaidWebhookRejectedException();
        }
    }

    #region Private Methods

    /// <summary>
    /// Validates the ES256 signature and returns the token.
    /// </summary>
    private static async Task<JsonWebToken> ReadSignedToken(string jwt, JsonWebKey key)
    {
        if (string.IsNullOrWhiteSpace(jwt)
            || !string.Equals(key.Alg, SecurityAlgorithms.EcdsaSha256, StringComparison.Ordinal))
        {
            throw new PlaidWebhookRejectedException();
        }

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(
            jwt,
            new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidAlgorithms = [SecurityAlgorithms.EcdsaSha256],
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false,
                RequireExpirationTime = false,
                RequireSignedTokens = true
            });

        if (!result.IsValid || result.SecurityToken is not JsonWebToken token)
        {
            throw new PlaidWebhookRejectedException();
        }

        return token;
    }

    /// <summary>
    /// Accepts an issued-at time within five minutes, allowing one minute of clock skew.
    /// </summary>
    private static bool IssuedAtIsCurrent(JsonWebToken token, DateTimeOffset utcNow)
    {
        if (!TryReadIssuedAt(token, out var issuedAt))
        {
            return false;
        }

        var age = utcNow - DateTimeOffset.FromUnixTimeSeconds(issuedAt);
        return age >= TimeSpan.FromMinutes(-1) && age <= MaximumAge;
    }

    /// <summary>
    /// Compares Plaid's claimed SHA-256 body hash with the raw request bytes.
    /// </summary>
    private static bool BodyHashMatches(JsonWebToken token, ReadOnlySpan<byte> rawBody)
    {
        if (!token.TryGetPayloadValue<string>("request_body_sha256", out var claimed)
            || string.IsNullOrWhiteSpace(claimed))
        {
            return false;
        }

        var actual = Convert.ToHexString(SHA256.HashData(rawBody)).ToLowerInvariant();
        var claimedBytes = Encoding.ASCII.GetBytes(claimed.Trim().ToLowerInvariant());
        var actualBytes = Encoding.ASCII.GetBytes(actual);
        return claimedBytes.Length == actualBytes.Length
            && CryptographicOperations.FixedTimeEquals(claimedBytes, actualBytes);
    }

    /// <summary>
    /// Reads the iat claim as a unix second count.
    /// </summary>
    private static bool TryReadIssuedAt(JsonWebToken token, out long issuedAt)
    {
        if (token.TryGetPayloadValue<long>("iat", out issuedAt))
        {
            return true;
        }

        if (token.TryGetPayloadValue<double>("iat", out var numeric))
        {
            issuedAt = (long)numeric;
            return true;
        }

        issuedAt = 0;
        return false;
    }

    #endregion
}
