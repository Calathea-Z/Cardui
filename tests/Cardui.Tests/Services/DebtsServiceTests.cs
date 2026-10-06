using Cardui.Api.Data;
using Cardui.Api.Domain;
using Cardui.Api.Dtos.Debts;
using Cardui.Api.Exceptions;
using Cardui.Api.Models;
using Cardui.Api.Security;
using Cardui.Api.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Cardui.Tests.Services;

public class DebtsServiceTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 5, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Create_StoresThePlanningCurrencyAndLeavesUnknownTermsEmpty()
    {
        await using var dbContext = CreateDbContext();
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        await SetPlanningCurrencyAsync(dbContext, householdId, "CAD");
        var accountId = await AddAccountAsync(dbContext, householdId, "Store card", 900m);
        var service = CreateService(dbContext, Bind(householdId));

        var saved = await service.CreateAsync(Card(accountId));

        Assert.Equal("Store card", saved.Name);
        Assert.Equal(DebtKind.Revolving, saved.Kind);
        Assert.Equal(accountId, saved.AccountId);
        Assert.Equal("Store card", saved.AccountName);
        Assert.Equal(842.50m, saved.Balance);
        Assert.Equal(new DateOnly(2026, 10, 1), saved.BalanceAsOf);
        Assert.Equal("CAD", saved.Currency);
        Assert.Null(saved.Apr);
        Assert.Null(saved.MinimumPayment);
        Assert.Null(saved.NextDueDate);
        Assert.Null(saved.CreditLimit);
        Assert.Null(saved.RemainingTermMonths);
        Assert.Null(saved.PromotionalApr);
        Assert.Null(saved.PromotionalEndsOn);
        Assert.Null(saved.Utilization);
        Assert.Equal(900m, await AccountBalanceAsync(dbContext, accountId));
    }

    [Fact]
    public async Task Create_CalculatesUtilizationAndDropsATermThatDoesNotApply()
    {
        await using var dbContext = CreateDbContext();
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var service = CreateService(dbContext, Bind(householdId));
        var card = Card(null);
        card.CreditLimit = 1000m;
        card.RemainingTermMonths = 36;

        var revolving = await service.CreateAsync(card);

        Assert.Equal(1000m, revolving.CreditLimit);
        Assert.Null(revolving.RemainingTermMonths);
        Assert.Equal(0.8425m, revolving.Utilization);

        var loan = Card(null);
        loan.Name = "Car loan";
        loan.Kind = DebtKind.Installment;
        loan.CreditLimit = 1000m;
        loan.RemainingTermMonths = 36;
        var installment = await service.CreateAsync(loan);

        Assert.Null(installment.CreditLimit);
        Assert.Equal(36, installment.RemainingTermMonths);
        Assert.Null(installment.Utilization);
    }

    [Fact]
    public async Task Create_KeepsAKnownZeroDistinctFromUnknown()
    {
        await using var dbContext = CreateDbContext();
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var service = CreateService(dbContext, Bind(householdId));
        var card = Card(null);
        card.Balance = 0m;
        card.Apr = 0m;
        card.MinimumPayment = 0m;
        card.PromotionalApr = 0m;

        var saved = await service.CreateAsync(card);

        Assert.Equal(0m, saved.Balance);
        Assert.Equal(0m, saved.Apr);
        Assert.Equal(0m, saved.MinimumPayment);
        Assert.Equal(0m, saved.PromotionalApr);
        Assert.Null(saved.PromotionalEndsOn);
        Assert.Equal("USD", saved.Currency);
    }

    [Fact]
    public async Task Create_RejectsADuplicateNameAForeignAccountAndABalanceWithoutADate()
    {
        await using var dbContext = CreateDbContext();
        var firstId = await CreateHouseholdAsync(dbContext, "user_owner_a");
        var secondId = await CreateHouseholdAsync(dbContext, "user_owner_b");
        var foreignAccountId = await AddAccountAsync(dbContext, secondId, "Other card", 10m);
        var first = CreateService(dbContext, Bind(firstId));

        await first.CreateAsync(Card(null));

        var duplicate = Card(null);
        duplicate.Name = "store card";
        var duplicateError = await Assert.ThrowsAsync<BadRequestException>(
            () => first.CreateAsync(duplicate));
        Assert.Equal("That debt is already in the household.", duplicateError.Message);

        var foreign = Card(foreignAccountId);
        foreign.Name = "Other";
        var foreignError = await Assert.ThrowsAsync<BadRequestException>(
            () => first.CreateAsync(foreign));
        Assert.Equal("Choose an account from this household.", foreignError.Message);

        var undated = Card(null);
        undated.Name = "Undated";
        undated.BalanceAsOf = null;
        var undatedError = await Assert.ThrowsAsync<BadRequestException>(
            () => first.CreateAsync(undated));
        Assert.Equal("Enter the date this balance was true.", undatedError.Message);
        Assert.Single(await first.GetDebtsAsync());
    }

    [Fact]
    public async Task Debts_StayInsideTheHousehold()
    {
        await using var dbContext = CreateDbContext();
        var firstId = await CreateHouseholdAsync(dbContext, "user_owner_a");
        var secondId = await CreateHouseholdAsync(dbContext, "user_owner_b");
        var first = CreateService(dbContext, Bind(firstId));
        var saved = await first.CreateAsync(Card(null));

        var second = CreateService(dbContext, Bind(secondId));
        Assert.Empty(await second.GetDebtsAsync());
        await Assert.ThrowsAsync<NotFoundException>(() =>
            second.UpdateAsync(saved.Id, Card(null)));
    }

    [Fact]
    public async Task Delete_RemovesTheDebtAndLeavesTheAccountBalance()
    {
        await using var dbContext = CreateDbContext();
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var otherId = await CreateHouseholdAsync(dbContext, "user_owner_b");
        var accountId = await AddAccountAsync(dbContext, householdId, "Store card", 900m);
        var service = CreateService(dbContext, Bind(householdId));
        var saved = await service.CreateAsync(Card(accountId));

        await service.DeleteAsync(saved.Id);
        Assert.Empty(await service.GetDebtsAsync());
        Assert.Equal(900m, await AccountBalanceAsync(dbContext, accountId));

        var again = await service.CreateAsync(Card(accountId));
        Assert.Equal("Store card", again.Name);

        var other = CreateService(dbContext, Bind(otherId));
        await Assert.ThrowsAsync<NotFoundException>(() => other.DeleteAsync(again.Id));
        Assert.Single(await service.GetDebtsAsync());
    }

    [Fact]
    public async Task Update_KeepsTheCurrencyAndCanClearAKnownTerm()
    {
        await using var dbContext = CreateDbContext();
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var accountId = await AddAccountAsync(dbContext, householdId, "Store card", 900m);
        var service = CreateService(dbContext, Bind(householdId));
        var card = Card(accountId);
        card.Apr = 19.99m;
        card.CreditLimit = 2000m;
        var saved = await service.CreateAsync(card);
        await SetPlanningCurrencyAsync(dbContext, householdId, "EUR");

        var updated = Card(null);
        updated.Kind = DebtKind.Installment;
        updated.Balance = null;
        updated.BalanceAsOf = null;
        updated.Apr = null;
        updated.RemainingTermMonths = 24;
        updated.CreditLimit = 2000m;
        var result = await service.UpdateAsync(saved.Id, updated);

        Assert.Equal("USD", result.Currency);
        Assert.Null(result.AccountId);
        Assert.Null(result.AccountName);
        Assert.Null(result.Balance);
        Assert.Null(result.BalanceAsOf);
        Assert.Null(result.Apr);
        Assert.Null(result.CreditLimit);
        Assert.Equal(24, result.RemainingTermMonths);
        Assert.Null(result.Utilization);
        Assert.Equal(DebtKind.Installment, result.Kind);
        Assert.Equal(900m, await AccountBalanceAsync(dbContext, accountId));
    }

    [Fact]
    public async Task ArchiveAccount_LeavesTheDebtLinkedToThatAccount()
    {
        await using var dbContext = CreateDbContext();
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var scope = Bind(householdId);
        var accountId = await AddAccountAsync(dbContext, householdId, "Store card", 900m);
        var service = CreateService(dbContext, scope);
        var saved = await service.CreateAsync(Card(accountId));

        await new AccountsService(dbContext, new FakeTimeProvider(Now), scope)
            .ArchiveAccountAsync(accountId);

        var listed = Assert.Single(await service.GetDebtsAsync());
        Assert.Equal(saved.Id, listed.Id);
        Assert.Equal(accountId, listed.AccountId);
        Assert.Equal("Store card", listed.AccountName);
        Assert.Equal(842.50m, listed.Balance);
        Assert.Equal(900m, await AccountBalanceAsync(dbContext, accountId));

        var updated = Card(accountId);
        updated.Balance = 800m;
        var result = await service.UpdateAsync(saved.Id, updated);
        Assert.Equal(accountId, result.AccountId);
        Assert.Equal(800m, result.Balance);
        Assert.Equal(900m, await AccountBalanceAsync(dbContext, accountId));
    }

    [Fact]
    public async Task Summary_ShowsBothBalancesAndKeepsTheDebtBalanceUntilChosen()
    {
        await using var dbContext = CreateDbContext();
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var accountId = await AddAccountAsync(
            dbContext,
            householdId,
            "Store card",
            950m,
            currency: "USD");
        await AddSnapshotAsync(dbContext, accountId, new DateOnly(2026, 9, 1), 800m, "USD");
        await AddSnapshotAsync(dbContext, accountId, new DateOnly(2026, 10, 5), 900m, "USD");
        var service = CreateService(dbContext, Bind(householdId));
        var card = Card(accountId);
        card.Apr = 19.99m;
        var saved = await service.CreateAsync(card);

        var summary = await service.GetSummaryAsync();
        var item = Assert.Single(summary.Debts);
        Assert.Equal(saved.Id, item.DebtId);
        Assert.Equal(14.03m, item.MonthlyInterest);
        Assert.NotNull(item.BalanceComparison);
        Assert.Equal(900m, item.BalanceComparison.AccountBalance);
        Assert.Equal(new DateOnly(2026, 10, 5), item.BalanceComparison.AccountBalanceAsOf);
        Assert.True(item.BalanceComparison.CanUseAccountBalance);
        var totals = Assert.Single(summary.Currencies);
        Assert.Equal(842.50m, totals.RecordedBalance);

        var chosen = await service.UseAccountBalanceAsync(saved.Id);

        Assert.Equal(900m, chosen.Balance);
        Assert.Equal(new DateOnly(2026, 10, 5), chosen.BalanceAsOf);
        Assert.Equal(19.99m, chosen.Apr);
        Assert.Null(chosen.MinimumPayment);
        Assert.Null(chosen.NextDueDate);
        Assert.Equal(950m, await AccountBalanceAsync(dbContext, accountId));

        var after = Assert.Single((await service.GetSummaryAsync()).Debts);
        Assert.Equal(14.99m, after.MonthlyInterest);
        Assert.Null(after.BalanceComparison);
    }

    [Fact]
    public async Task Summary_DoesNotCreateADebtFromAnAccount()
    {
        await using var dbContext = CreateDbContext();
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var otherId = await CreateHouseholdAsync(dbContext, "user_owner_b");
        var accountId = await AddAccountAsync(dbContext, householdId, "Store card", 900m, currency: "USD");
        await AddSnapshotAsync(dbContext, accountId, new DateOnly(2026, 10, 5), 900m, "USD");
        var service = CreateService(dbContext, Bind(householdId));

        var summary = await service.GetSummaryAsync();

        Assert.Empty(summary.Debts);
        Assert.Empty(await service.GetDebtsAsync());

        var cashId = await AddAccountAsync(
            dbContext,
            householdId,
            "Checking",
            100m,
            AccountTypes.Depository,
            "USD");
        var debt = await service.CreateAsync(Card(cashId));
        var compared = Assert.Single((await service.GetSummaryAsync()).Debts);
        Assert.Null(compared.BalanceComparison);

        var cashError = await Assert.ThrowsAsync<BadRequestException>(
            () => service.UseAccountBalanceAsync(debt.Id));
        Assert.Equal(
            "That account balance is not an amount owed, so it is not copied.",
            cashError.Message);
        Assert.Equal(842.50m, (await service.GetDebtsAsync()).Single().Balance);

        var undatedId = await AddAccountAsync(
            dbContext,
            householdId,
            "Other card",
            700m,
            currency: "USD");
        var undatedDebt = Card(undatedId);
        undatedDebt.Name = "Other card";
        var undated = await service.CreateAsync(undatedDebt);
        var blocked = Assert.Single(
            (await service.GetSummaryAsync()).Debts,
            item => item.DebtId == undated.Id);
        Assert.NotNull(blocked.BalanceComparison);
        Assert.Equal(DebtAccountBalanceBlock.DateUnknown, blocked.BalanceComparison.Block);
        var dateError = await Assert.ThrowsAsync<BadRequestException>(
            () => service.UseAccountBalanceAsync(undated.Id));
        Assert.Equal("That account balance has no date, so it is not copied.", dateError.Message);

        var other = CreateService(dbContext, Bind(otherId));
        await Assert.ThrowsAsync<NotFoundException>(
            () => other.UseAccountBalanceAsync(debt.Id));
        Assert.Empty((await other.GetSummaryAsync()).Debts);
    }

    private static UpsertDebtDto Card(Guid? accountId)
    {
        return new UpsertDebtDto
        {
            Name = "  Store card  ",
            Kind = DebtKind.Revolving,
            AccountId = accountId,
            Balance = 842.50m,
            BalanceAsOf = new DateOnly(2026, 10, 1)
        };
    }

    private static DebtsService CreateService(CarduiDBContext dbContext, HouseholdScope scope)
    {
        return new DebtsService(dbContext, new FakeTimeProvider(Now), scope);
    }

    private static HouseholdScope Bind(Guid householdId)
    {
        var scope = new HouseholdScope();
        scope.Bind(householdId);
        return scope;
    }

    private static async Task<Guid> CreateHouseholdAsync(CarduiDBContext dbContext, string ownerId)
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
        string name,
        decimal currentBalance,
        string type = AccountTypes.Credit,
        string? currency = null)
    {
        var account = new Account
        {
            Id = Guid.NewGuid(),
            HouseholdId = householdId,
            Source = FinancialRecordSource.Manual,
            Provenance = FinancialRecordProvenance.ManualEntry,
            Name = name,
            Type = type,
            IsoCurrencyCode = currency,
            IsActive = true,
            CurrentBalance = currentBalance,
            CreatedAt = Now,
            UpdatedAt = Now
        };
        dbContext.Accounts.Add(account);
        await dbContext.SaveChangesAsync();
        return account.Id;
    }

    private static async Task AddSnapshotAsync(
        CarduiDBContext dbContext,
        Guid accountId,
        DateOnly date,
        decimal currentBalance,
        string currency)
    {
        dbContext.AccountBalanceSnapshots.Add(new AccountBalanceSnapshot
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Date = date,
            CurrentBalance = currentBalance,
            IsoCurrencyCode = currency,
            CreatedAt = Now
        });
        await dbContext.SaveChangesAsync();
    }

    private static async Task<decimal> AccountBalanceAsync(CarduiDBContext dbContext, Guid accountId)
    {
        return await dbContext.Accounts
            .AsNoTracking()
            .Where(account => account.Id == accountId)
            .Select(account => account.CurrentBalance)
            .SingleAsync();
    }

    private static CarduiDBContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CarduiDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CarduiDBContext(options);
    }
}
