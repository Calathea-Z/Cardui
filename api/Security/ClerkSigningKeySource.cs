using System.Security.Cryptography;
using Cardui.Api.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Cardui.Api.Security;

public sealed class ClerkSigningKeySource : IClerkSigningKeySource
{
    private readonly IOptionsMonitor<ClerkOptions> _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _cacheGate = new(1, 1);
    private IReadOnlyList<SecurityKey>? _cachedKeys;
    private DateTimeOffset _cacheExpiresAt;

    public ClerkSigningKeySource(
        IOptionsMonitor<ClerkOptions> options,
        IHttpClientFactory httpClientFactory,
        TimeProvider timeProvider)
    {
        _options = options;
        _httpClientFactory = httpClientFactory;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<SecurityKey>> GetSigningKeysAsync(
        CancellationToken cancellationToken = default)
    {
        var options = _options.CurrentValue;
        if (!string.IsNullOrWhiteSpace(options.JwtPublicKey))
        {
            return [CreatePemKey(options.JwtPublicKey)];
        }

        var now = _timeProvider.GetUtcNow();
        if (_cachedKeys is not null && now < _cacheExpiresAt)
        {
            return _cachedKeys;
        }

        await _cacheGate.WaitAsync(cancellationToken);
        try
        {
            now = _timeProvider.GetUtcNow();
            if (_cachedKeys is not null && now < _cacheExpiresAt)
            {
                return _cachedKeys;
            }

            var issuer = options.Issuer;
            if (string.IsNullOrWhiteSpace(issuer))
            {
                throw new ClerkSessionTokenException(
                    "Clerk session verification is not configured.");
            }

            var jwksUri = ClerkJwksAddress.Create(issuer);
            var client = _httpClientFactory.CreateClient(ClerkAuthenticationDefaults.Scheme);
            var json = await client.GetStringAsync(jwksUri, cancellationToken);
            var keys = new JsonWebKeySet(json).GetSigningKeys().ToList();
            if (keys.Count == 0)
            {
                throw new ClerkSessionTokenException(
                    "Clerk signing keys could not be loaded.");
            }

            _cachedKeys = keys;
            _cacheExpiresAt = now.AddHours(1);
            return _cachedKeys;
        }
        catch (ClerkSessionTokenException)
        {
            throw;
        }
        catch (Exception exception) when (exception is HttpRequestException or System.Text.Json.JsonException)
        {
            throw new ClerkSessionTokenException("Clerk signing keys could not be loaded.");
        }
        finally
        {
            _cacheGate.Release();
        }
    }

    public static SecurityKey CreatePemKey(string pem)
    {
        var normalized = pem.Replace("\\n", "\n", StringComparison.Ordinal).Trim();
        var rsa = RSA.Create();
        rsa.ImportFromPem(normalized);
        return new RsaSecurityKey(rsa);
    }
}
