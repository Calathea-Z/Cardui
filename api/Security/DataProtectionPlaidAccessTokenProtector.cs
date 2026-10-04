using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;

namespace Cardui.Api.Security;

public class DataProtectionPlaidAccessTokenProtector : IPlaidAccessTokenProtector
{
    private const string ProtectedTokenPrefix = "dp:v1:";
    private const string Purpose = "Cardui.Plaid.AccessToken.v1";

    private readonly IDataProtector _protector;
    private readonly ILogger<DataProtectionPlaidAccessTokenProtector> _logger;

    public DataProtectionPlaidAccessTokenProtector(
        IDataProtectionProvider dataProtectionProvider,
        ILogger<DataProtectionPlaidAccessTokenProtector> logger)
    {
        _protector = dataProtectionProvider.CreateProtector(Purpose);
        _logger = logger;
    }

    /// <inheritdoc />
    public string Protect(string accessToken)
    {
        return $"{ProtectedTokenPrefix}{_protector.Protect(accessToken)}";
    }

    /// <inheritdoc />
    public string Unprotect(string storedAccessToken)
    {
        if (!storedAccessToken.StartsWith(ProtectedTokenPrefix, StringComparison.Ordinal))
        {
            return storedAccessToken;
        }

        try
        {
            return _protector.Unprotect(storedAccessToken[ProtectedTokenPrefix.Length..]);
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "Failed to unprotect Plaid access token");
            throw;
        }
    }
}
