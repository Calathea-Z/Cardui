using System.Text;
using Cardui.Api.Data;
using Cardui.Api.Domain.Accounts;
using Cardui.Api.Domain.Categories;
using Cardui.Api.Domain.TransactionImport;
using Cardui.Api.Dtos.Transaction;
using Cardui.Api.Dtos.TransactionImport;
using Cardui.Api.Exceptions;
using Cardui.Api.Models;
using Cardui.Api.Security;
using Cardui.Api.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Cardui.Tests.Services;

public class TransactionImportServiceTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 4, 15, 0, 0, TimeSpan.Zero);

    private static readonly Guid HouseholdA =
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private static readonly Guid HouseholdB =
        Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private const string SampleCsv =
        """
        Date,Description,Amount,Category,Notes
        2026-10-01,Starbucks,4.50,,
        2026-10-01,Starbucks,4.50,,
        2026-10-01,Rent,100.00,Groceries,October
        2026-10-03,Paycheck,-2000.00,,
        2025-12-01,Old,5.00,,
        bad,Missing,1.00,,
        """;

    [Fact]
    public async Task Inspect_SuggestsColumnsFromTheHeader()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, HouseholdA);

        var inspected = await service.InspectAsync(Csv(SampleCsv), "export.csv");

        Assert.Equal("export.csv", inspected.FileName);
        Assert.Equal(6, inspected.DataRowCount);
        Assert.Equal(0, inspected.Suggested.DateColumn);
        Assert.Equal(1, inspected.Suggested.NameColumn);
        Assert.Equal(2, inspected.Suggested.AmountColumn);
        Assert.Equal(3, inspected.Suggested.CategoryColumn);
        Assert.Equal(4, inspected.Suggested.NotesColumn);
    }

    [Fact]
    public async Task Commit_ImportsReadyRowsAndUndoRestoresTheBalance()
    {
        await using var dbContext = CreateDbContext();
        var accountId = await SeedAccountAsync(dbContext, HouseholdA);
        await SeedRentAsync(dbContext, accountId);
        await SeedCategoriesAsync(dbContext);
        var service = CreateService(dbContext, HouseholdA);
        var transactions = CreateTransactionsService(dbContext, HouseholdA);

        var preview = await service.PreviewAsync(Csv(SampleCsv), "export.csv", Request(accountId));

        Assert.Equal(2, preview.ReadyCount);
        Assert.Equal(2, preview.DuplicateCount);
        Assert.Equal(2, preview.ErrorCount);
        Assert.Equal("Food & Dining", preview.Rows[0].CategoryName);
        Assert.Equal(TransactionImportRowStatus.Duplicate, preview.Rows[1].Status);
        Assert.Equal(TransactionImportRowStatus.Duplicate, preview.Rows[2].Status);
        Assert.Contains("opening date", preview.Rows[4].Message);

        var imported = await service.CommitAsync(
            Csv(SampleCsv),
            "export.csv",
            Request(accountId, "2,5"));

        Assert.Equal(2, imported.ImportedCount);
        var stored = await dbContext.Transactions
            .Where(transaction => transaction.ImportId == imported.Id)
            .OrderBy(transaction => transaction.Date)
            .ToListAsync();
        Assert.Equal(["Starbucks", "Paycheck"], stored.Select(transaction => transaction.Name));
        Assert.All(stored, transaction =>
        {
            Assert.Equal(FinancialRecordSource.Csv, transaction.Source);
            Assert.Equal(FinancialRecordProvenance.CsvImport, transaction.Provenance);
            Assert.False(transaction.IsCategoryUserEdited);
        });
        Assert.Equal(2895.50m, (await dbContext.Accounts.SingleAsync()).CurrentBalance);

        var again = await service.PreviewAsync(Csv(SampleCsv), "export.csv", Request(accountId));
        Assert.Equal(TransactionImportRowStatus.Duplicate, again.Rows[0].Status);

        var edited = await transactions.UpdateTransactionDetailsAsync(
            stored[0].Id,
            new UpdateTransactionDetailsDto
            {
                Date = new DateOnly(2026, 10, 1),
                Name = "Starbucks Reserve",
                Amount = 5m,
                Notes = "Corrected"
            });
        Assert.Equal("Starbucks Reserve", edited.Name);
        Assert.Equal(5m, edited.Amount);

        var open = await service.ListOpenAsync();
        Assert.Equal(imported.Id, Assert.Single(open).Id);

        var undone = await service.UndoAsync(imported.Id);
        Assert.NotNull(undone.UndoneAt);
        Assert.Equal(900m, (await dbContext.Accounts.SingleAsync()).CurrentBalance);
        Assert.All(
            await dbContext.Transactions.Where(transaction => transaction.ImportId == imported.Id).ToListAsync(),
            transaction => Assert.NotNull(transaction.ArchivedAt));
        Assert.Empty(await service.ListOpenAsync());
        await Assert.ThrowsAsync<BadRequestException>(() => service.UndoAsync(imported.Id));
    }

    [Fact]
    public async Task Commit_CanIncludeADuplicateTheUserChecked()
    {
        await using var dbContext = CreateDbContext();
        var accountId = await SeedAccountAsync(dbContext, HouseholdA);
        await SeedRentAsync(dbContext, accountId);
        await SeedCategoriesAsync(dbContext);
        var service = CreateService(dbContext, HouseholdA);

        await service.CommitAsync(Csv(SampleCsv), "export.csv", Request(accountId, "4"));

        var rent = await dbContext.Transactions.SingleAsync(transaction => transaction.Name == "Rent" && transaction.ImportId != null);
        Assert.Equal(groceriesFrom(dbContext), rent.CategoryId);
        Assert.True(rent.IsCategoryUserEdited);
        Assert.Equal("October", rent.Notes);
    }

    [Fact]
    public async Task Commit_RejectsAnErrorLine()
    {
        await using var dbContext = CreateDbContext();
        var accountId = await SeedAccountAsync(dbContext, HouseholdA);
        var service = CreateService(dbContext, HouseholdA);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            service.CommitAsync(Csv(SampleCsv), "export.csv", Request(accountId, "7")));
    }

    [Fact]
    public async Task Preview_RejectsAnotherHouseholdAccount()
    {
        await using var dbContext = CreateDbContext();
        var accountId = await SeedAccountAsync(dbContext, HouseholdB);
        var service = CreateService(dbContext, HouseholdA);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.PreviewAsync(Csv(SampleCsv), "export.csv", Request(accountId)));
    }

    [Fact]
    public async Task Undo_RejectsAnotherHousehold()
    {
        await using var dbContext = CreateDbContext();
        var accountId = await SeedAccountAsync(dbContext, HouseholdA);
        await SeedCategoriesAsync(dbContext);
        var imported = await CreateService(dbContext, HouseholdA)
            .CommitAsync(Csv(SampleCsv), "export.csv", Request(accountId, "2"));

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateService(dbContext, HouseholdB).UndoAsync(imported.Id));
    }

    [Fact]
    public async Task Inspect_RejectsAFileOverTheSizeLimit()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, HouseholdA);
        var bytes = new byte[TransactionImportLimits.MaxFileBytes + 1];
        Array.Fill(bytes, (byte)'a');

        var error = await Assert.ThrowsAsync<BadRequestException>(() =>
            service.InspectAsync(new MemoryStream(bytes), "export.csv"));

        Assert.Contains("1 MB", error.Message);
    }

    private static Guid groceriesFrom(CarduiDBContext dbContext)
    {
        return dbContext.Categories.Single(category => category.Name == "Groceries").Id;
    }

    private static TransactionImportRequestDto Request(Guid accountId, string? lines = null)
    {
        return new TransactionImportRequestDto
        {
            AccountId = accountId,
            DateColumn = 0,
            NameColumn = 1,
            AmountColumn = 2,
            CategoryColumn = 3,
            NotesColumn = 4,
            AmountSign = CsvAmountSign.PositiveOut,
            DateOrder = CsvDateOrder.MonthFirst,
            IncludedLineNumbers = lines
        };
    }

    private static MemoryStream Csv(string text) => new(Encoding.UTF8.GetBytes(text));

    private static CarduiDBContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CarduiDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CarduiDBContext(options);
    }

    private static TransactionImportService CreateService(CarduiDBContext dbContext, Guid householdId)
    {
        return new TransactionImportService(dbContext, new FakeTimeProvider(Now), Bind(householdId));
    }

    private static TransactionsService CreateTransactionsService(
        CarduiDBContext dbContext,
        Guid householdId)
    {
        return new TransactionsService(dbContext, new FakeTimeProvider(Now), Bind(householdId));
    }

    private static HouseholdScope Bind(Guid householdId)
    {
        var scope = new HouseholdScope();
        scope.Bind(householdId);
        return scope;
    }

    private static async Task<Guid> SeedAccountAsync(CarduiDBContext dbContext, Guid householdId)
    {
        var accountId = Guid.NewGuid();
        dbContext.Accounts.Add(new Account
        {
            Id = accountId,
            HouseholdId = householdId,
            Name = "Checking",
            Type = AccountTypes.Depository,
            Source = FinancialRecordSource.Manual,
            Provenance = FinancialRecordProvenance.ManualEntry,
            CurrentBalance = 1000m,
            OpeningBalance = 1000m,
            OpeningBalanceDate = new DateOnly(2026, 1, 1),
            IsoCurrencyCode = "USD",
            CreatedAt = Now,
            UpdatedAt = Now
        });
        await dbContext.SaveChangesAsync();
        return accountId;
    }

    private static async Task SeedRentAsync(CarduiDBContext dbContext, Guid accountId)
    {
        dbContext.Transactions.Add(new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            PlaidTransactionId = null,
            Source = FinancialRecordSource.Manual,
            Provenance = FinancialRecordProvenance.ManualEntry,
            Date = new DateOnly(2026, 10, 1),
            Name = "Rent",
            MerchantName = "Rent",
            Amount = 100m,
            IsoCurrencyCode = "USD",
            Pending = false,
            CreatedAt = Now,
            UpdatedAt = Now
        });
        await dbContext.SaveChangesAsync();
    }

    private static async Task<Guid> SeedCategoriesAsync(CarduiDBContext dbContext)
    {
        var groupId = Guid.NewGuid();
        var subGroupId = Guid.NewGuid();
        var foodId = Guid.NewGuid();
        var incomeId = Guid.NewGuid();
        var groceriesId = Guid.NewGuid();

        dbContext.Groups.Add(new Group
        {
            Id = groupId,
            Key = "expenses",
            Name = "Expenses",
            SortOrder = 1,
            CreatedAt = Now,
            UpdatedAt = Now
        });
        dbContext.SubGroups.Add(new SubGroup
        {
            Id = subGroupId,
            GroupId = groupId,
            Key = "daily",
            Name = "Daily",
            IsSystem = true,
            SortOrder = 0,
            CreatedAt = Now,
            UpdatedAt = Now
        });
        dbContext.Categories.AddRange(
            new Category
            {
                Id = foodId,
                SubGroupId = subGroupId,
                Key = SystemCategoryKeys.FoodDining,
                Name = "Food & Dining",
                IsSystem = true,
                CreatedAt = Now,
                UpdatedAt = Now
            },
            new Category
            {
                Id = incomeId,
                SubGroupId = subGroupId,
                Key = SystemCategoryKeys.Income,
                Name = "Income",
                IsSystem = true,
                CreatedAt = Now,
                UpdatedAt = Now
            },
            new Category
            {
                Id = groceriesId,
                HouseholdId = HouseholdA,
                SubGroupId = subGroupId,
                Key = "groceries",
                Name = "Groceries",
                IsSystem = false,
                CreatedAt = Now,
                UpdatedAt = Now
            });
        await dbContext.SaveChangesAsync();
        return groceriesId;
    }
}
