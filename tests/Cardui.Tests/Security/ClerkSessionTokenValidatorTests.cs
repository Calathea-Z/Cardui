using System.Security.Claims;
using System.Security.Cryptography;
using Cardui.Api.Options;
using Cardui.Api.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Cardui.Tests.Security;

public class ClerkSessionTokenValidatorTests
{
    private const string Issuer = "https://example.clerk.accounts.dev";
    private const string OwnerId = "user_owner";
    private const string AuthorizedParty = "http://localhost:3000";

    [Fact]
    public async Task Validate_AcceptsATokenForTheConfiguredParty()
    {
        using var rsa = RSA.Create(2048);
        var validator = CreateValidator(rsa);

        var principal = await validator.ValidateAsync(CreateToken(rsa, OwnerId, AuthorizedParty));

        Assert.Equal(
            OwnerId,
            principal.FindFirst(ClerkAuthenticationDefaults.UserIdClaimType)?.Value);
    }

    [Fact]
    public async Task Validate_RejectsADifferentAuthorizedParty()
    {
        using var rsa = RSA.Create(2048);
        var validator = CreateValidator(rsa);

        await Assert.ThrowsAsync<ClerkSessionTokenException>(() =>
            validator.ValidateAsync(CreateToken(rsa, OwnerId, "https://evil.example")));
    }

    [Fact]
    public async Task Validate_RejectsATokenSignedByAnotherKey()
    {
        using var trusted = RSA.Create(2048);
        using var other = RSA.Create(2048);
        var validator = CreateValidator(trusted);

        await Assert.ThrowsAsync<ClerkSessionTokenException>(() =>
            validator.ValidateAsync(CreateToken(other, OwnerId, AuthorizedParty)));
    }

    [Fact]
    public async Task Validate_RejectsAnExpiredToken()
    {
        using var rsa = RSA.Create(2048);
        var validator = CreateValidator(rsa);
        var token = CreateToken(
            rsa,
            OwnerId,
            AuthorizedParty,
            expires: DateTime.UtcNow.AddMinutes(-10));

        await Assert.ThrowsAsync<ClerkSessionTokenException>(() =>
            validator.ValidateAsync(token));
    }

    [Fact]
    public void JwksAddress_RequiresAnHttpsOrigin()
    {
        Assert.Equal(
            "https://example.clerk.accounts.dev/.well-known/jwks.json",
            ClerkJwksAddress.Create(Issuer).AbsoluteUri);

        Assert.Throws<ClerkSessionTokenException>(() =>
            ClerkJwksAddress.Create("http://example.clerk.accounts.dev"));
    }

    private static ClerkSessionTokenValidator CreateValidator(RSA rsa)
    {
        return new ClerkSessionTokenValidator(
            new FixedOptionsMonitor(new ClerkOptions
            {
                Issuer = Issuer,
                AuthorizedParties = [AuthorizedParty, "https://localhost:3000"]
            }),
            new StaticSigningKeySource(new RsaSecurityKey(rsa)));
    }

    private static string CreateToken(
        RSA rsa,
        string userId,
        string authorizedParty,
        DateTime? expires = null)
    {
        var handler = new JsonWebTokenHandler();
        return handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Expires = expires ?? DateTime.UtcNow.AddMinutes(5),
            Subject = new ClaimsIdentity(
            [
                new Claim(ClerkAuthenticationDefaults.UserIdClaimType, userId),
                new Claim("azp", authorizedParty)
            ]),
            SigningCredentials = new SigningCredentials(
                new RsaSecurityKey(rsa),
                SecurityAlgorithms.RsaSha256)
        });
    }

    private sealed class StaticSigningKeySource : IClerkSigningKeySource
    {
        private readonly SecurityKey _key;

        public StaticSigningKeySource(SecurityKey key)
        {
            _key = key;
        }

        public Task<IReadOnlyList<SecurityKey>> GetSigningKeysAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<SecurityKey>>([_key]);
        }
    }

    private sealed class FixedOptionsMonitor : IOptionsMonitor<ClerkOptions>
    {
        public FixedOptionsMonitor(ClerkOptions currentValue)
        {
            CurrentValue = currentValue;
        }

        public ClerkOptions CurrentValue { get; }

        public ClerkOptions Get(string? name) => CurrentValue;

        public IDisposable OnChange(Action<ClerkOptions, string?> listener) =>
            new NoopDisposable();

        private sealed class NoopDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
