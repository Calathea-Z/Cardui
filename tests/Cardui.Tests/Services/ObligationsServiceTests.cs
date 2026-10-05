using Cardui.Api.Data;
using Cardui.Api.Domain;
using Cardui.Api.Dtos.Obligations;
using Cardui.Api.Exceptions;
using Cardui.Api.Models;
using Cardui.Api.Security;
using Cardui.Api.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Cardui.Tests.Services;

public class ObligationsServiceTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 5, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Create_StoresOnePaymentInThePlanningCurrency()
    {
        await using var dbContext = CreateDbContext();
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        await SetPlanningCurrencyAsync(dbContext, householdId, "CAD");
        var accountId = await AddAccountAsync(dbContext, householdId, "Checking");
        var service = CreateService(dbContext, Bind(householdId));

        var saved = await service.CreateAsync(Rent(accountId));

        Assert.Equal("Rent", saved.Name);
        Assert.Equal(1450.50m, saved.Amount);
        Assert.Equal("CAD", saved.Currency);
        Assert.Equal(ObligationCadence.Monthly, saved.Cadence);
        Assert.Equal(new DateOnly(2026, 10, 1), saved.NextDueDate);
        Assert.Equal(accountId, saved.AccountId);
        Assert.Equal("Checking", saved.AccountName);
        Assert.Equal(ObligationFlexibility.Essential, saved.Flexibility);
    }

    [Fact]
    public async Task Create_AllowsABillWithNoAccount()
    {
        await using var dbContext = CreateDbContext();
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var service = CreateService(dbContext, Bind(householdId));

        var saved = await service.CreateAsync(Rent(null));

        Assert.Null(saved.AccountId);
        Assert.Null(saved.AccountName);
        Assert.Equal("USD", saved.Currency);
    }

    [Fact]
    public async Task Create_RejectsADuplicateNameAndAForeignAccount()
    {
        await using var dbContext = CreateDbContext();
        var firstId = await CreateHouseholdAsync(dbContext, "user_owner_a");
        var secondId = await CreateHouseholdAsync(dbContext, "user_owner_b");
        var foreignAccountId = await AddAccountAsync(dbContext, secondId, "Other checking");
        var first = CreateService(dbContext, Bind(firstId));

        await first.CreateAsync(Rent(null));

        var duplicate = Rent(null);
        duplicate.Name = "rent";
        var duplicateError = await Assert.ThrowsAsync<BadRequestException>(
            () => first.CreateAsync(duplicate));
        Assert.Equal("That bill is already in the household.", duplicateError.Message);

        var foreign = Rent(foreignAccountId);
        foreign.Name = "Power";
        var foreignError = await Assert.ThrowsAsync<BadRequestException>(
            () => first.CreateAsync(foreign));
        Assert.Equal("Choose an account from this household.", foreignError.Message);
        Assert.Single(await first.GetObligationsAsync());
    }

    [Fact]
    public async Task Create_RejectsAnAmountThatIsNotOnePayment()
    {
        await using var dbContext = CreateDbContext();
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var service = CreateService(dbContext, Bind(householdId));
        var bill = Rent(null);
        bill.Amount = 0m;

        var error = await Assert.ThrowsAsync<BadRequestException>(() => service.CreateAsync(bill));
        Assert.Equal("Enter the amount for one payment.", error.Message);
        Assert.Empty(await service.GetObligationsAsync());
    }

    [Fact]
    public async Task Bills_StayInsideTheHousehold()
    {
        await using var dbContext = CreateDbContext();
        var firstId = await CreateHouseholdAsync(dbContext, "user_owner_a");
        var secondId = await CreateHouseholdAsync(dbContext, "user_owner_b");
        var first = CreateService(dbContext, Bind(firstId));
        var saved = await first.CreateAsync(Rent(null));

        var second = CreateService(dbContext, Bind(secondId));
        Assert.Empty(await second.GetObligationsAsync());
        await Assert.ThrowsAsync<NotFoundException>(() =>
            second.UpdateAsync(saved.Id, Rent(null)));
    }

    [Fact]
    public async Task Delete_RemovesTheBillAndFreesTheName()
    {
        await using var dbContext = CreateDbContext();
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var otherId = await CreateHouseholdAsync(dbContext, "user_owner_b");
        var accountId = await AddAccountAsync(dbContext, householdId, "Checking");
        var service = CreateService(dbContext, Bind(householdId));
        var saved = await service.CreateAsync(Rent(accountId));

        await service.DeleteAsync(saved.Id);
        Assert.Empty(await service.GetObligationsAsync());
        Assert.NotNull(await dbContext.Accounts.SingleAsync(account => account.Id == accountId));

        var again = await service.CreateAsync(Rent(accountId));
        Assert.Equal("Rent", again.Name);

        var other = CreateService(dbContext, Bind(otherId));
        await Assert.ThrowsAsync<NotFoundException>(() => other.DeleteAsync(again.Id));
        Assert.Single(await service.GetObligationsAsync());
    }

    [Fact]
    public async Task Update_KeepsTheCurrencyAndCanClearTheAccount()
    {
        await using var dbContext = CreateDbContext();
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var accountId = await AddAccountAsync(dbContext, householdId, "Checking");
        var service = CreateService(dbContext, Bind(householdId));
        var saved = await service.CreateAsync(Rent(accountId));
        await SetPlanningCurrencyAsync(dbContext, householdId, "EUR");

        var updated = Rent(null);
        updated.Amount = 1500m;
        updated.Flexibility = ObligationFlexibility.Flexible;
        var result = await service.UpdateAsync(saved.Id, updated);

        Assert.Equal(1500m, result.Amount);
        Assert.Equal("USD", result.Currency);
        Assert.Null(result.AccountId);
        Assert.Null(result.AccountName);
        Assert.Equal(ObligationFlexibility.Flexible, result.Flexibility);
        Assert.NotNull(await dbContext.Accounts.SingleAsync(account => account.Id == accountId));
    }

    [Fact]
    public async Task ArchiveAccount_LeavesTheBillLinkedToThatAccount()
    {
        await using var dbContext = CreateDbContext();
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var scope = Bind(householdId);
        var accountId = await AddAccountAsync(dbContext, householdId, "Checking");
        var service = CreateService(dbContext, scope);
        var saved = await service.CreateAsync(Rent(accountId));

        await new AccountsService(dbContext, new FakeTimeProvider(Now), scope)
            .ArchiveAccountAsync(accountId);

        var listed = Assert.Single(await service.GetObligationsAsync());
        Assert.Equal(saved.Id, listed.Id);
        Assert.Equal(accountId, listed.AccountId);
        Assert.Equal("Checking", listed.AccountName);
        Assert.Equal(1450.50m, listed.Amount);

        var updated = Rent(accountId);
        updated.Amount = 1460m;
        var result = await service.UpdateAsync(saved.Id, updated);
        Assert.Equal(accountId, result.AccountId);
        Assert.Equal(1460m, result.Amount);
    }

    private static UpsertObligationDto Rent(Guid? accountId)
    {
        return new UpsertObligationDto
        {
            Name = "  Rent  ",
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
