using Cardui.Api.Data;
using Cardui.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace Cardui.Api.Services.Plaid;

public class PlaidAccessTokenStoreMigrator
{
    private readonly CarduiDBContext _dbContext;
    private readonly IPlaidAccessTokenProtector _currentProtector;
    private readonly ILogger<PlaidAccessTokenStoreMigrator> _logger;

    public PlaidAccessTokenStoreMigrator(
        CarduiDBContext dbContext,
        IPlaidAccessTokenProtector currentProtector,
        ILogger<PlaidAccessTokenStoreMigrator> logger)
    {
        _dbContext = dbContext;
        _currentProtector = currentProtector;
        _logger = logger;
    }

    /// <summary>
    /// Rewraps every plaintext Plaid access token with the current key ring.
    /// A value that already opens with that ring is left unchanged.
    /// Token values are not written to the log.
    /// </summary>
    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        var items = await _dbContext.PlaidItems.ToListAsync(cancellationToken);
        var changed = 0;

        foreach (var item in items)
        {
            if (TryRewrap(item.AccessToken, out var protectedToken))
            {
                item.AccessToken = protectedToken;
                changed++;
            }
        }

        if (changed > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Reprotected {Count} Plaid access tokens.", changed);
    }

    #region Private Methods

    /// <summary>
    /// Returns a current ciphertext when the stored value is plaintext.
    /// A value that already opens with the current ring is left unchanged.
    /// </summary>
    private bool TryRewrap(string storedAccessToken, out string protectedToken)
    {
        protectedToken = string.Empty;
        if (!storedAccessToken.StartsWith(PlaidAccessTokenCipher.Prefix, StringComparison.Ordinal))
        {
            protectedToken = _currentProtector.Protect(storedAccessToken);
            return true;
        }

        try
        {
            _currentProtector.Unprotect(storedAccessToken);
            return false;
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            throw new InvalidOperationException(
                "A stored Plaid access token could not be decrypted. Reconnect that bank.");
        }
    }

    #endregion
}
