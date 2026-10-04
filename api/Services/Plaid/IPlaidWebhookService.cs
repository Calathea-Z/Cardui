namespace Cardui.Api.Services.Plaid;

public interface IPlaidWebhookService
{
    /// <summary>
    /// Verifies a Plaid webhook and applies a revoked-consent or connection warning.
    /// The raw body is hashed for verification and is not logged.
    /// </summary>
    Task AcceptAsync(
        string verificationJwt,
        ReadOnlyMemory<byte> rawBody,
        CancellationToken cancellationToken = default);
}
