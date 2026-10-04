using Microsoft.IdentityModel.Tokens;

namespace Cardui.Api.Security;

public interface IPlaidWebhookKeySource
{
    /// <summary>
    /// Returns the Plaid verification key for this key id.
    /// Expired keys are rejected.
    /// </summary>
    Task<JsonWebKey> GetKeyAsync(string keyId, CancellationToken cancellationToken = default);
}
