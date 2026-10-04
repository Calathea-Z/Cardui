using Cardui.Api.Data;
using Cardui.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Cardui.Api.Services.Plaid;

public class PlaidItemRemoval
{
    private readonly CarduiDBContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public PlaidItemRemoval(CarduiDBContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Deletes the stored bank login and detaches its accounts.
    /// Transactions stay on those accounts.
    /// </summary>
    public async Task RemoveStoredItemAsync(
        PlaidItem plaidItem,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var accounts = await _dbContext.Accounts
            .Where(account => account.PlaidItemId == plaidItem.Id)
            .ToListAsync(cancellationToken);

        foreach (var account in accounts)
        {
            account.PlaidItemId = null;
            account.UpdatedAt = now;
        }

        _dbContext.PlaidItems.Remove(plaidItem);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
