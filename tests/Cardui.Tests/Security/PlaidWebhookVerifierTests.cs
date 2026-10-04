using System.Security.Cryptography;
using System.Text;
using Cardui.Api.Exceptions;
using Cardui.Api.Security;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Cardui.Tests.Security;

public class PlaidWebhookVerifierTests
{
    [Fact]
    public async Task Verify_AcceptsACurrentSignedBody()
    {
        var now = DateTimeOffset.Parse("2026-10-04T12:00:00Z");
        var body = Encoding.UTF8.GetBytes("{\"webhook_type\":\"ITEM\"}");
        var (jwt, key) = Sign(body, now);

        await PlaidWebhookVerifier.Verify(jwt, body, key, now);

        Assert.Equal("test-key", PlaidWebhookVerifier.ReadKeyId(jwt));
    }

    [Fact]
    public async Task Verify_RejectsADifferentBody()
    {
        var now = DateTimeOffset.Parse("2026-10-04T12:00:00Z");
        var (jwt, key) = Sign(Encoding.UTF8.GetBytes("{\"webhook_type\":\"ITEM\"}"), now);

        await Assert.ThrowsAsync<PlaidWebhookRejectedException>(() =>
            PlaidWebhookVerifier.Verify(
                jwt,
                Encoding.UTF8.GetBytes("{\"webhook_type\":\"CHANGED\"}"),
                key,
                now));
    }

    [Fact]
    public async Task Verify_RejectsAnOldToken()
    {
        var issuedAt = DateTimeOffset.Parse("2026-10-04T12:00:00Z");
        var body = Encoding.UTF8.GetBytes("{}");
        var (jwt, key) = Sign(body, issuedAt);

        await Assert.ThrowsAsync<PlaidWebhookRejectedException>(() =>
            PlaidWebhookVerifier.Verify(jwt, body, key, issuedAt.AddMinutes(6)));
    }

    [Fact]
    public void ReadKeyId_RejectsAnUnsignedToken()
    {
        Assert.Throws<PlaidWebhookRejectedException>(() =>
            PlaidWebhookVerifier.ReadKeyId("not-a-token"));
    }

    private static (string Jwt, JsonWebKey Key) Sign(byte[] body, DateTimeOffset issuedAt)
    {
        var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var parameters = ecdsa.ExportParameters(false);
        var key = new JsonWebKey
        {
            Kty = "EC",
            Crv = "P-256",
            X = Base64UrlEncoder.Encode(parameters.Q.X),
            Y = Base64UrlEncoder.Encode(parameters.Q.Y),
            Alg = SecurityAlgorithms.EcdsaSha256,
            Kid = "test-key",
            Use = "sig"
        };
        var hash = Convert.ToHexString(SHA256.HashData(body)).ToLowerInvariant();
        var jwt = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            IssuedAt = issuedAt.UtcDateTime,
            Claims = new Dictionary<string, object>
            {
                ["request_body_sha256"] = hash
            },
            SigningCredentials = new SigningCredentials(
                new ECDsaSecurityKey(ecdsa) { KeyId = "test-key" },
                SecurityAlgorithms.EcdsaSha256)
        });
        return (jwt, key);
    }
}
