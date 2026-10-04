using System.Collections.Concurrent;
using Cardui.Api.Exceptions;
using Cardui.Api.Services.Plaid;
using Going.Plaid.WebhookVerificationKey;
using Microsoft.IdentityModel.Tokens;

namespace Cardui.Api.Security;

public sealed class PlaidWebhookKeySource : IPlaidWebhookKeySource
{
    private readonly IPlaidClientSource _clientSource;
    private readonly IPlaidRequestExecutor _requestExecutor;
    private readonly TimeProvider _timeProvider;
    private readonly ConcurrentDictionary<string, (JsonWebKey Key, DateTimeOffset ExpiresAt)> _cache = new();

    public PlaidWebhookKeySource(
        IPlaidClientSource clientSource,
        IPlaidRequestExecutor requestExecutor,
        TimeProvider timeProvider)
    {
        _clientSource = clientSource;
        _requestExecutor = requestExecutor;
        _timeProvider = timeProvider;
    }

    /// <inheritdoc />
    public async Task<JsonWebKey> GetKeyAsync(
        string keyId,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        if (_cache.TryGetValue(keyId, out var cached) && now < cached.ExpiresAt)
        {
            return cached.Key;
        }

        var client = _clientSource.GetClient();
        var request = _requestExecutor.WithCredentials(new WebhookVerificationKeyGetRequest
        {
            KeyId = keyId
        });
        var response = await _requestExecutor.ExecuteAsync(
            () => client.WebhookVerificationKeyGetAsync(request));
        var key = CreateKey(response.Key, now);
        var expiresAt = now.AddHours(12);
        _cache[keyId] = (key, expiresAt);
        return key;
    }

    #region Private Methods

    /// <summary>
    /// Copies an unexpired Plaid EC key into a JSON web key that can verify ES256.
    /// </summary>
    private static JsonWebKey CreateKey(Going.Plaid.Entity.JWKPublicKey plaidKey, DateTimeOffset now)
    {
        if (plaidKey.ExpiredAt is int expiredAt
            && DateTimeOffset.FromUnixTimeSeconds(expiredAt) <= now)
        {
            throw new PlaidWebhookRejectedException();
        }

        if (!string.Equals(plaidKey.Kty, "EC", StringComparison.Ordinal)
            || !string.Equals(plaidKey.Crv, "P-256", StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(plaidKey.X)
            || string.IsNullOrWhiteSpace(plaidKey.Y))
        {
            throw new PlaidWebhookRejectedException();
        }

        return new JsonWebKey
        {
            Kty = plaidKey.Kty,
            Crv = plaidKey.Crv,
            X = plaidKey.X,
            Y = plaidKey.Y,
            Kid = plaidKey.Kid,
            Use = "sig",
            Alg = SecurityAlgorithms.EcdsaSha256
        };
    }

    #endregion
}
