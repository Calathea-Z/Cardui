using Cardui.Api.Data;
using Cardui.Api.Models;
using Cardui.Api.Services.Plaid;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cardui.Tests.Services;

public class PlaidItemRemovalTests
{
    [Fact]
    public async Task RemoveStoredItem_DeletesTheTokenAndKeepsTransactions()
    {
        await using var dbContext = CreateDbContext();
        var itemId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        var item = new PlaidItem
        {
            Id = itemId,
            HouseholdId = Guid.NewGuid(),
            PlaidItemId = "item-1",
            AccessToken = "dp:v1:stored",
            CreatedAt = DateTimeOffset.UnixEpoch,
            UpdatedAt = DateTimeOffset.UnixEpoch
        };
        dbContext.PlaidItems.Add(item);
        dbContext.Accounts.Add(new Account
        {
            Id = accountId,
            PlaidItemId = itemId,
            PlaidAccountId = "account-1",
            Name = "Checking",
            Type = "depository",
            CreatedAt = DateTimeOffset.UnixEpoch,
            UpdatedAt = DateTimeOffset.UnixEpoch
        });
        dbContext.Transactions.Add(new Transaction
        {
            Id = transactionId,
            AccountId = accountId,
            PlaidTransactionId = "txn-1",
            Date = new DateOnly(2026, 10, 1),
            Name = "Market",
            Amount = 12m,
            CreatedAt = DateTimeOffset.UnixEpoch,
            UpdatedAt = DateTimeOffset.UnixEpoch
        });
        await dbContext.SaveChangesAsync();

        var removal = new PlaidItemRemoval(dbContext, TimeProvider.System);
        await removal.RemoveStoredItemAsync(item);

        Assert.False(await dbContext.PlaidItems.AnyAsync());
        Assert.Null((await dbContext.Accounts.SingleAsync()).PlaidItemId);
        Assert.Equal(transactionId, (await dbContext.Transactions.SingleAsync()).Id);
    }

    private static CarduiDBContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CarduiDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CarduiDBContext(options);
    }
}
