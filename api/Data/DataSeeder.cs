using Cardui.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Cardui.Api.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(CarduiDBContext dbContext)
    {
        if (await dbContext.Categories.AnyAsync())
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;

        var categories = new List<Category>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Income",
                Color = "#16a34a",
                Icon = "banknote",
                IsSystem = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Groceries",
                Color = "#22c55e",
                Icon = "shopping-basket",
                IsSystem = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Dining",
                Color = "#f97316",
                Icon = "utensils",
                IsSystem = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Bills",
                Color = "#6366f1",
                Icon = "receipt",
                IsSystem = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Transport",
                Color = "#0ea5e9",
                Icon = "car",
                IsSystem = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Shopping",
                Color = "#ec4899",
                Icon = "shopping-bag",
                IsSystem = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Entertainment",
                Color = "#a855f7",
                Icon = "ticket",
                IsSystem = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Transfers",
                Color = "#64748b",
                Icon = "repeat",
                IsSystem = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Uncategorized",
                Color = "#71717a",
                Icon = "circle-help",
                IsSystem = true,
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        dbContext.Categories.AddRange(categories);

        var plaidItem = new PlaidItem
        {
            Id = Guid.NewGuid(),
            PlaidItemId = "demo-plaid-item",
            AccessToken = "demo-access-token",
            InstitutionId = "demo-ins",
            InstitutionName = "Demo Bank",
            CreatedAt = now,
            UpdatedAt = now
        };

        var checkingAccount = new Account
        {
            Id = Guid.NewGuid(),
            PlaidItem = plaidItem,
            PlaidAccountId = "demo-checking",
            Name = "Everyday Checking",
            OfficialName = "Demo Bank Everyday Checking",
            Type = "depository",
            Subtype = "checking",
            Mask = "1234",
            CurrentBalance = 4280.55m,
            AvailableBalance = 4100.55m,
            IsoCurrencyCode = "USD",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        var creditCardAccount = new Account
        {
            Id = Guid.NewGuid(),
            PlaidItem = plaidItem,
            PlaidAccountId = "demo-credit-card",
            Name = "Rewards Credit Card",
            OfficialName = "Demo Bank Rewards Visa",
            Type = "credit",
            Subtype = "credit card",
            Mask = "9876",
            CurrentBalance = 1264.33m,
            AvailableBalance = 8735.67m,
            IsoCurrencyCode = "USD",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.PlaidItems.Add(plaidItem);
        dbContext.Accounts.AddRange(checkingAccount, creditCardAccount);

        Category Category(string name) =>
            categories.Single(x => x.Name == name);

        var transactions = new List<Transaction>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Account = checkingAccount,
                PlaidTransactionId = "demo-txn-paycheck-1",
                Date = DateOnly.FromDateTime(DateTime.Today.AddDays(-3)),
                AuthorizedDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-3)),
                Name = "Payroll Deposit",
                MerchantName = "Acme Corp",
                Amount = -3200.00m,
                IsoCurrencyCode = "USD",
                Pending = false,
                Category = Category("Income"),
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Account = checkingAccount,
                PlaidTransactionId = "demo-txn-rent",
                Date = DateOnly.FromDateTime(DateTime.Today.AddDays(-2)),
                AuthorizedDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-2)),
                Name = "Rent Payment",
                MerchantName = "Property Management",
                Amount = 1650.00m,
                IsoCurrencyCode = "USD",
                Pending = false,
                Category = Category("Bills"),
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Account = creditCardAccount,
                PlaidTransactionId = "demo-txn-grocery-1",
                Date = DateOnly.FromDateTime(DateTime.Today.AddDays(-5)),
                AuthorizedDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-5)),
                Name = "Whole Foods Market",
                MerchantName = "Whole Foods",
                Amount = 84.27m,
                IsoCurrencyCode = "USD",
                Pending = false,
                Category = Category("Groceries"),
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Account = creditCardAccount,
                PlaidTransactionId = "demo-txn-dining-1",
                Date = DateOnly.FromDateTime(DateTime.Today.AddDays(-6)),
                AuthorizedDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-6)),
                Name = "Bluebird Cafe",
                MerchantName = "Bluebird Cafe",
                Amount = 42.18m,
                IsoCurrencyCode = "USD",
                Pending = false,
                Category = Category("Dining"),
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Account = creditCardAccount,
                PlaidTransactionId = "demo-txn-fuel",
                Date = DateOnly.FromDateTime(DateTime.Today.AddDays(-8)),
                AuthorizedDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-8)),
                Name = "Shell",
                MerchantName = "Shell",
                Amount = 51.64m,
                IsoCurrencyCode = "USD",
                Pending = false,
                Category = Category("Transport"),
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Account = creditCardAccount,
                PlaidTransactionId = "demo-txn-streaming",
                Date = DateOnly.FromDateTime(DateTime.Today.AddDays(-11)),
                AuthorizedDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-11)),
                Name = "Netflix",
                MerchantName = "Netflix",
                Amount = 22.99m,
                IsoCurrencyCode = "USD",
                Pending = false,
                Category = Category("Entertainment"),
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Account = creditCardAccount,
                PlaidTransactionId = "demo-txn-shopping",
                Date = DateOnly.FromDateTime(DateTime.Today.AddDays(-14)),
                AuthorizedDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-14)),
                Name = "Target",
                MerchantName = "Target",
                Amount = 136.42m,
                IsoCurrencyCode = "USD",
                Pending = false,
                Category = Category("Shopping"),
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Account = checkingAccount,
                PlaidTransactionId = "demo-txn-transfer",
                Date = DateOnly.FromDateTime(DateTime.Today.AddDays(-16)),
                AuthorizedDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-16)),
                Name = "Transfer to Savings",
                MerchantName = "Demo Bank",
                Amount = 500.00m,
                IsoCurrencyCode = "USD",
                Pending = false,
                Category = Category("Transfers"),
                CreatedAt = now,
                UpdatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Account = creditCardAccount,
                PlaidTransactionId = "demo-txn-pending",
                Date = DateOnly.FromDateTime(DateTime.Today),
                AuthorizedDate = DateOnly.FromDateTime(DateTime.Today),
                Name = "Coffee Shop",
                MerchantName = "Coffee Shop",
                Amount = 6.75m,
                IsoCurrencyCode = "USD",
                Pending = true,
                Category = Category("Dining"),
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        dbContext.Transactions.AddRange(transactions);

        await dbContext.SaveChangesAsync();
    }
}