using System.Security.Claims;
using Cardui.Api.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Cardui.Api.Security;

public sealed class ClerkSessionTokenValidator
{
    private static readonly TimeSpan ClockSkew = TimeSpan.FromSeconds(30);

    private readonly IOptionsMonitor<ClerkOptions> _options;
    private readonly IClerkSigningKeySource _signingKeys;
    private readonly JsonWebTokenHandler _tokenHandler = new();

    public ClerkSessionTokenValidator(
        IOptionsMonitor<ClerkOptions> options,
        IClerkSigningKeySource signingKeys)
    {
        _options = options;
        _signingKeys = signingKeys;
    }

    /// <summary>
    /// Validates a Clerk session token and returns a principal whose name is the Clerk user id.
    /// The issuer, lifetime, signature, and authorized party must match configuration.
    /// </summary>
    public async Task<ClaimsPrincipal> ValidateAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ClerkSessionTokenException("A session token is required.");
        }

        var options = _options.CurrentValue;
        var issuer = options.Issuer?.Trim();
        if (string.IsNullOrWhiteSpace(issuer))
        {
            throw new ClerkSessionTokenException(
                "Clerk session verification is not configured.");
        }

        var keys = await _signingKeys.GetSigningKeysAsync(cancellationToken);
        var result = await _tokenHandler.ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = keys,
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = ClockSkew,
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256]
        });

        if (!result.IsValid)
        {
            throw new ClerkSessionTokenException("The session token is invalid.");
        }

        var userId = ReadStringClaim(result, ClerkAuthenticationDefaults.UserIdClaimType);
        var authorizedParty = ReadStringClaim(result, "azp");
        if (userId is null || authorizedParty is null)
        {
            throw new ClerkSessionTokenException("The session token is invalid.");
        }

        if (!options.AuthorizedParties.Contains(authorizedParty, StringComparer.Ordinal))
        {
            throw new ClerkSessionTokenException("The session token is invalid.");
        }

        var identity = new ClaimsIdentity(
            [new Claim(ClerkAuthenticationDefaults.UserIdClaimType, userId)],
            authenticationType: ClerkAuthenticationDefaults.Scheme,
            nameType: ClerkAuthenticationDefaults.UserIdClaimType,
            roleType: ClaimTypes.Role);

        return new ClaimsPrincipal(identity);
    }

    #region Private Methods

    /// <summary>
    /// Reads a string claim, or null when it is missing or blank.
    /// </summary>
    private static string? ReadStringClaim(TokenValidationResult result, string claimType)
    {
        if (!result.Claims.TryGetValue(claimType, out var value) || value is not string text)
        {
            return null;
        }

        var trimmed = text.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }

    #endregion
}
