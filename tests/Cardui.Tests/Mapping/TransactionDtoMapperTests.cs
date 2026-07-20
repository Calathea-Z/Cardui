using Cardui.Api.Mapping;
using Cardui.Api.Models;
using Xunit;

namespace Cardui.Tests.Mapping;

public class TransactionDtoMapperTests
{
    [Fact]
    public void Projection_MapsAccountAndCategoryDetails()
    {
        var now = DateTimeOffset.UtcNow;
        var accountId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Account = new Account
            {
                Id = accountId,
                PlaidItemId = Guid.NewGuid(),
                PlaidAccountId = "plaid-account-1",
                Name = "Everyday Checking",
                Type = "depository",
                Subtype = "checking",
                CreatedAt = now,
                UpdatedAt = now
            },
            PlaidTransactionId = "plaid-transaction-1",
            Date = new DateOnly(2026, 7, 15),
            AuthorizedDate = new DateOnly(2026, 7, 14),
            Name = "Coffee Shop",
            MerchantName = "Coffee Shop",
            Amount = 5.25m,
            IsoCurrencyCode = "USD",
            Pending = true,
            CategoryId = categoryId,
            Category = new Category
            {
                Id = categoryId,
                Key = "dining",
                Name = "Dining",
                Color = "#f97316",
                Icon = "utensils",
                IsSystem = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            CreatedAt = now,
            UpdatedAt = now
        };

        var dto = TransactionDtoMapper.Projection.Compile()(transaction);

        Assert.Equal(transaction.Id, dto.Id);
        Assert.Equal("Coffee Shop", dto.Name);
        Assert.Equal(5.25m, dto.Amount);
        Assert.True(dto.Pending);
        Assert.Equal(accountId, dto.Account.Id);
        Assert.Equal("Everyday Checking", dto.Account.Name);
        Assert.Equal(categoryId, dto.Category?.Id);
        Assert.Equal("Dining", dto.Category?.Name);
        Assert.Equal("#f97316", dto.Category?.Color);
    }

    [Fact]
    public void Projection_AllowsUncategorizedTransactions()
    {
        var now = DateTimeOffset.UtcNow;
        var accountId = Guid.NewGuid();

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Account = new Account
            {
                Id = accountId,
                PlaidItemId = Guid.NewGuid(),
                PlaidAccountId = "plaid-account-2",
                Name = "Credit Card",
                Type = "credit",
                CreatedAt = now,
                UpdatedAt = now
            },
            PlaidTransactionId = "plaid-transaction-2",
            Date = new DateOnly(2026, 7, 16),
            Name = "Unmatched Purchase",
            Amount = 42m,
            Pending = false,
            CreatedAt = now,
            UpdatedAt = now
        };

        var dto = TransactionDtoMapper.Projection.Compile()(transaction);

        Assert.Null(dto.Category);
        Assert.Equal("Credit Card", dto.Account.Name);
    }
}
