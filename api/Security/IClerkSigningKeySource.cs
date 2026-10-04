using Microsoft.IdentityModel.Tokens;

namespace Cardui.Api.Security;

public interface IClerkSigningKeySource
{
    /// <summary>
    /// Returns the keys used to verify Clerk session tokens. A configured
    /// PEM key is used as-is. Otherwise the issuer's JWKS document is loaded
    /// and cached for one hour.
    /// </summary>
    Task<IReadOnlyList<SecurityKey>> GetSigningKeysAsync(
        CancellationToken cancellationToken = default);
}
