using Microsoft.AspNetCore.DataProtection;

namespace Cardui.Api.Security;

public class DataProtectionPlaidAccessTokenProtector : IPlaidAccessTokenProtector
{
    private readonly IDataProtector _protector;

    public DataProtectionPlaidAccessTokenProtector(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector(PlaidAccessTokenCipher.Purpose);
    }

    /// <inheritdoc />
    public string Protect(string accessToken)
    {
        return PlaidAccessTokenCipher.Protect(_protector, accessToken);
    }

    /// <inheritdoc />
    public string Unprotect(string storedAccessToken)
    {
        return PlaidAccessTokenCipher.Unprotect(_protector, storedAccessToken);
    }
}
