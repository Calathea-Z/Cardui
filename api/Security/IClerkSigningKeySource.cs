using Microsoft.IdentityModel.Tokens;

namespace Cardui.Api.Security;

public interface IClerkSigningKeySource
{
    Task<IReadOnlyList<SecurityKey>> GetSigningKeysAsync(
        CancellationToken cancellationToken = default);
}
