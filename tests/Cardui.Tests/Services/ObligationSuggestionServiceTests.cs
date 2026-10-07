using Cardui.Api.Data;
using Cardui.Api.Domain.Accounts;
using Cardui.Api.Dtos.Obligations;
using Cardui.Api.Exceptions;
using Cardui.Api.Models;
using Cardui.Api.Security;
using Cardui.Api.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Cardui.Tests.Services;

public class ObligationSuggestionServiceTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 5, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetSuggestions_NoticesAMonthlyPaymentAndDoesNotCreateABill()
    {
        await using var dbContext = CreateDbContext();
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var accountId = await AddAccountAsync(dbContext, householdId, "Checking");
        await AddChargeAsync(dbContext, accountId, new DateOnly(2026, 8, 1), "aug");
        await AddChargeAsync(dbContext, accountId, new DateOnly(2026, 9, 1), "sep");
        await AddChargeAsync(dbContext, accountId, new DateOnly(2026, 10, 1), "oct");
        var service = CreateService(dbContext, Bind(householdId));

        var suggestion = Assert.Single(await service.GetSuggestionsAsync());

        Assert.Equal("netflix", suggestion.Key);
        Assert.Equal("Netflix", suggestion.Name);
        Assert.Equal(15.99m, suggestion.Amount);
        Assert.Equal("USD", suggestion.Currency);
        Assert.Equal(ObligationCadence.Monthly, suggestion.Cadence);
        Assert.Equal(new DateOnly(2026, 11, 1), suggestion.NextDueDate);
        Assert.Equal(accountId, suggestion.AccountId);
        Assert.Equal("Checking", suggestion.AccountName);
        Assert.Empty(await service.GetObligationsAsync());
    }

    [Fact]
    public async Task GetSuggestions_StaysInsideTheHouseholdAndThePlanningCurrency()
    {
        await using var dbContext = CreateDbContext();
        var firstId = await CreateHouseholdAsync(dbContext, "user_owner_a");
        var secondId = await CreateHouseholdAsync(dbContext, "user_owner_b");
        await SetPlanningCurrencyAsync(dbContext, firstId, "CAD");
        var firstAccountId = await AddAccountAsync(dbContext, firstId, "Chequing");
        var secondAccountId = await AddAccountAsync(dbContext, secondId, "Checking");
        await AddChargeAsync(dbContext, firstAccountId, new DateOnly(2026, 8, 1), "cad-aug", "CAD");
        await AddChargeAsync(dbContext, firstAccountId, new DateOnly(2026, 9, 1), "cad-sep", "CAD");
        await AddChargeAsync(dbContext, firstAccountId, new DateOnly(2026, 10, 1), "cad-oct", "CAD");
        await AddChargeAsync(dbContext, firstAccountId, new DateOnly(2026, 8, 1), "usd-aug", "USD", "Hulu");
        await AddChargeAsync(dbContext, firstAccountId, new DateOnly(2026, 9, 1), "usd-sep", "USD", "Hulu");
        await AddChargeAsync(dbContext, firstAccountId, new DateOnly(2026, 10, 1), "usd-oct", "USD", "Hulu");
        await AddChargeAsync(dbContext, secondAccountId, new DateOnly(2026, 8, 1), "other-aug", "USD", "Spotify");
        await AddChargeAsync(dbContext, secondAccountId, new DateOnly(2026, 9, 1), "other-sep", "USD", "Spotify");
        await AddChargeAsync(dbContext, secondAccountId, new DateOnly(2026, 10, 1), "other-oct", "USD", "Spotify");

        var first = Assert.Single(await CreateService(dbContext, Bind(firstId)).GetSuggestionsAsync());
        Assert.Equal("netflix", first.Key);
        Assert.Equal("CAD", first.Currency);

        var second = Assert.Single(await CreateService(dbContext, Bind(secondId)).GetSuggestionsAsync());
        Assert.Equal("spotify", second.Key);
    }

    [Fact]
    public async Task Create_StoresTheSuggestionKeyAndHidesThePatternUntilTheBillIsDeleted()
    {
        await using var dbContext = CreateDbContext();
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var accountId = await AddAccountAsync(dbContext, householdId, "Checking");
        await AddMonthlyNetflixAsync(dbContext, accountId);
        var service = CreateService(dbContext, Bind(householdId));
        var bill = Rent(null);
        bill.Name = "Streaming";
        bill.SuggestionKey = " Netflix ";

        var saved = await service.CreateAsync(bill);

        Assert.Empty(await service.GetSuggestionsAsync());
        var stored = await dbContext.Obligations.SingleAsync();
        Assert.Equal(saved.Id, stored.Id);
        Assert.Equal("netflix", stored.SuggestionKey);

        var edited = Rent(null);
        edited.Name = "Streaming";
        edited.Amount = 20m;
        edited.SuggestionKey = null;
        await service.UpdateAsync(saved.Id, edited);
        Assert.Equal("netflix", (await dbContext.Obligations.SingleAsync()).SuggestionKey);
        Assert.Empty(await service.GetSuggestionsAsync());

        await service.DeleteAsync(saved.Id);
        Assert.Equal("netflix", Assert.Single(await service.GetSuggestionsAsync()).Key);
    }

    [Fact]
    public async Task Dismiss_HidesThePatternWithoutCreatingABill()
    {
        await using var dbContext = CreateDbContext();
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var otherId = await CreateHouseholdAsync(dbContext, "user_owner_b");
        var accountId = await AddAccountAsync(dbContext, householdId, "Checking");
        await AddMonthlyNetflixAsync(dbContext, accountId);
        var service = CreateService(dbContext, Bind(householdId));

        await service.DismissSuggestionAsync(" Netflix ");
        await service.DismissSuggestionAsync("netflix");

        Assert.Empty(await service.GetSuggestionsAsync());
        Assert.Empty(await service.GetObligationsAsync());
        Assert.Equal("netflix", Assert.Single(await dbContext.ObligationSuggestionDismissals.ToListAsync()).Key);

        var otherAccountId = await AddAccountAsync(dbContext, otherId, "Other");
        await AddMonthlyNetflixAsync(dbContext, otherAccountId, "other");
        var other = Assert.Single(await CreateService(dbContext, Bind(otherId)).GetSuggestionsAsync());
        Assert.Equal("netflix", other.Key);
    }

    [Fact]
    public async Task Dismiss_RejectsABlankKey()
    {
        await using var dbContext = CreateDbContext();
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var service = CreateService(dbContext, Bind(householdId));

        var error = await Assert.ThrowsAsync<BadRequestException>(
            () => service.DismissSuggestionAsync("  "));

        Assert.Equal("That suggestion was not found.", error.Message);
        Assert.Empty(await dbContext.ObligationSuggestionDismissals.ToListAsync());
    }

    private static async Task AddMonthlyNetflixAsync(
        CarduiDBContext dbContext,
        Guid accountId,
        string prefix = "netflix")
    {
        await AddChargeAsync(dbContext, accountId, new DateOnly(2026, 8, 1), $"{prefix}-aug");
        await AddChargeAsync(dbContext, accountId, new DateOnly(2026, 9, 1), $"{prefix}-sep");
        await AddChargeAsync(dbContext, accountId, new DateOnly(2026, 10, 1), $"{prefix}-oct");
    }

    private static async Task AddChargeAsync(
        CarduiDBContext dbContext,
        Guid accountId,
        DateOnly date,
        string id,
        string? currency = "USD",
        string merchant = "Netflix")
    {
        dbContext.Transactions.Add(new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            PlaidTransactionId = $"transaction-{id}",
            Source = FinancialRecordSource.Manual,
            Provenance = FinancialRecordProvenance.ManualEntry,
            Date = date,
            Name = "Card purchase",
            MerchantName = merchant,
            Amount = 15.99m,
            IsoCurrencyCode = currency,
            CreatedAt = Now,
            UpdatedAt = Now
        });
        await dbContext.SaveChangesAsync();
    }

    private static UpsertObligationDto Rent(Guid? accountId)
    {
        return new UpsertObligationDto
        {
            Name = "Rent",
            Amount = 1450.50m,
            Cadence = ObligationCadence.Monthly,
            NextDueDate = new DateOnly(2026, 10, 1),
            AccountId = accountId,
            Flexibility = ObligationFlexibility.Essential
        };
    }

    private static ObligationsService CreateService(
        CarduiDBContext dbContext,
        HouseholdScope scope)
    {
        return new ObligationsService(dbContext, new FakeTimeProvider(Now), scope);
    }

    private static HouseholdScope Bind(Guid householdId)
    {
        var scope = new HouseholdScope();
        scope.Bind(householdId);
        return scope;
    }

    private static async Task<Guid> CreateHouseholdAsync(
        CarduiDBContext dbContext,
        string ownerId)
    {
        var household = await new HouseholdsService(dbContext, new FakeTimeProvider(Now))
            .GetOrCreateForOwnerAsync(ownerId);
        return household.Id;
    }

    private static async Task SetPlanningCurrencyAsync(
        CarduiDBContext dbContext,
        Guid householdId,
        string currency)
    {
        var household = await dbContext.Households.SingleAsync(x => x.Id == householdId);
        household.PlanningCurrency = currency;
        await dbContext.SaveChangesAsync();
    }

    private static async Task<Guid> AddAccountAsync(
        CarduiDBContext dbContext,
        Guid householdId,
        string name)
    {
        var account = new Account
        {
            Id = Guid.NewGuid(),
            HouseholdId = householdId,
            Source = FinancialRecordSource.Manual,
            Provenance = FinancialRecordProvenance.ManualEntry,
            Name = name,
            Type = AccountTypes.Depository,
            IsActive = true,
            CreatedAt = Now,
            UpdatedAt = Now
        };
        dbContext.Accounts.Add(account);
        await dbContext.SaveChangesAsync();
        return account.Id;
    }

    private static CarduiDBContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CarduiDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CarduiDBContext(options);
    }
}
