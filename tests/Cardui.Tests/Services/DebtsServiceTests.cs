using Cardui.Api.Data;
using Cardui.Api.Domain;
using Cardui.Api.Domain.Accounts;
using Cardui.Api.Domain.Debts;
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

    [Fact]
    public async Task Follow_ReadsTheSnapshotAndDoesNotWriteTheDebtBalance()
    {
        await using var dbContext = CreateDbContext();
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var accountId = await AddConnectedAccountAsync(dbContext, householdId, 900m, snapshot: 1240.18m);
        var service = CreateService(dbContext, Bind(householdId));
        var debt = await service.CreateAsync(Card(accountId));

        var choices = await service.GetFollowAccountsAsync(debt.Id);
        var choice = Assert.Single(choices);
        Assert.Equal(accountId, choice.AccountId);
        Assert.True(choice.BalancesDiffer);
        Assert.Equal(1240.18m, choice.BalanceInUse);

        var followed = await service.FollowAccountAsync(
            debt.Id,
            new FollowDebtAccountDto { AccountId = accountId });

        Assert.True(followed.Following);
        Assert.Equal(842.50m, followed.Balance);
        Assert.Equal(1240.18m, followed.BalanceInUse);
        Assert.Equal(DebtFieldSource.Synced, followed.BalanceSource);
        Assert.Equal(DebtLinkFreshness.Current, followed.Freshness);
        Assert.Equal(900m, await AccountBalanceAsync(dbContext, accountId));
        var summary = await service.GetSummaryAsync();
        Assert.Equal(1240.18m, Assert.Single(summary.Currencies).RecordedBalance);
        Assert.Null(Assert.Single(summary.Debts).BalanceComparison);

        var edited = Card(accountId);
        edited.Balance = 1m;
        edited.BalanceAsOf = new DateOnly(2026, 10, 2);
        edited.Apr = 9m;
        var saved = await service.UpdateAsync(debt.Id, edited);
        Assert.Equal(842.50m, saved.Balance);
        Assert.Equal(1240.18m, saved.BalanceInUse);
        Assert.Equal(9m, saved.Apr);

        var stopped = await service.StopFollowingAsync(debt.Id);
        Assert.False(stopped.Following);
        Assert.Equal(accountId, stopped.AccountId);
        Assert.Equal(1240.18m, stopped.Balance);
        Assert.Equal(new DateOnly(2026, 10, 5), stopped.BalanceAsOf);
        Assert.Equal(1240.18m, stopped.BalanceInUse);
    }

    [Fact]
    public async Task Follow_CanKeepTheRecordedBalanceAndRejectsASecondFollow()
    {
        await using var dbContext = CreateDbContext();
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var accountId = await AddConnectedAccountAsync(dbContext, householdId, 900m, snapshot: 1240.18m);
        var manualId = await AddAccountAsync(dbContext, householdId, "Cash", 20m);
        var service = CreateService(dbContext, Bind(householdId));
        var debt = await service.CreateAsync(Card(accountId));

        var kept = await service.FollowAccountAsync(
            debt.Id,
            new FollowDebtAccountDto { AccountId = accountId, KeepOwnBalance = true });
        Assert.Equal(DebtFieldSource.Override, kept.BalanceSource);
        Assert.Equal(842.50m, kept.BalanceInUse);
        Assert.Equal(1240.18m, kept.SyncedBalance);

        var other = Card(null);
        other.Name = "Other card";
        var second = await service.CreateAsync(other);
        var taken = await Assert.ThrowsAsync<BadRequestException>(
            () => service.FollowAccountAsync(
                second.Id,
                new FollowDebtAccountDto { AccountId = accountId }));
        Assert.Equal("That account is already followed by another debt.", taken.Message);

        var cash = await Assert.ThrowsAsync<BadRequestException>(
            () => service.FollowAccountAsync(
                second.Id,
                new FollowDebtAccountDto { AccountId = manualId }));
        Assert.Equal("Choose a connected credit card or loan in this currency.", cash.Message);

        await service.StopFollowingAsync(debt.Id);
        var again = await service.UseAccountBalanceAsync(debt.Id);
        Assert.True(again.Following);
        Assert.Equal(842.50m, again.Balance);
        Assert.Equal(1240.18m, again.BalanceInUse);
    }

    [Fact]
    public async Task UseAccountBalance_FollowsAnEligibleAccountAndCopiesAManualOne()
    {
        await using var dbContext = CreateDbContext();
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var accountId = await AddConnectedAccountAsync(dbContext, householdId, 900m, snapshot: 1240.18m);
        var service = CreateService(dbContext, Bind(householdId));
        var debt = await service.CreateAsync(Card(accountId));

        var followed = await service.UseAccountBalanceAsync(debt.Id);

        Assert.True(followed.Following);
        Assert.Equal(842.50m, followed.Balance);
        Assert.Equal(1240.18m, followed.BalanceInUse);
        Assert.Equal(DebtFieldSource.Synced, followed.BalanceSource);
        Assert.Equal(900m, await AccountBalanceAsync(dbContext, accountId));

        var other = Card(accountId);
        other.Name = "Other card";
        var second = await service.CreateAsync(other);
        var copied = await service.UseAccountBalanceAsync(second.Id);
        Assert.False(copied.Following);
        Assert.Equal(1240.18m, copied.Balance);
        Assert.Equal(new DateOnly(2026, 10, 5), copied.BalanceAsOf);
    }

    [Fact]
    public async Task Follow_CountsACardCreditAsZeroAndKeepsItOffTheDebtUntilStopped()
    {
        await using var dbContext = CreateDbContext();
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var accountId = await AddConnectedAccountAsync(dbContext, householdId, -42.10m, snapshot: -42.10m);
        var service = CreateService(dbContext, Bind(householdId));
        var created = await service.CreateAsync(Card(accountId));

        var followed = await service.FollowAccountAsync(
            created.Id,
            new FollowDebtAccountDto { AccountId = accountId });
        Assert.Equal(0m, followed.BalanceInUse);
        Assert.Equal(42.10m, followed.BalanceCredit);
        Assert.Equal(842.50m, followed.Balance);

        var stopped = await service.StopFollowingAsync(created.Id);
        Assert.Equal(0m, stopped.Balance);
        Assert.Equal(new DateOnly(2026, 10, 5), stopped.BalanceAsOf);
        Assert.Null(stopped.BalanceCredit);
    }

    [Fact]
    public async Task Override_KeepsThePersonsBalanceEvenWhenItMatchesAndCanReturnToSynced()
    {
        await using var dbContext = CreateDbContext();
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var accountId = await AddConnectedAccountAsync(dbContext, householdId, 900m, snapshot: 1240.18m);
        var service = CreateService(dbContext, Bind(householdId));
        var debt = await service.CreateAsync(Card(accountId));
        await service.FollowAccountAsync(
            debt.Id,
            new FollowDebtAccountDto { AccountId = accountId });

        var kept = await service.SetOverrideAsync(
            debt.Id,
            DebtSyncedField.Balance,
            new SetDebtBalanceOverrideDto
            {
                Balance = 1240.18m,
                BalanceAsOf = new DateOnly(2026, 10, 2)
            });
        Assert.Equal(DebtFieldSource.Override, kept.BalanceSource);
        Assert.Equal(1240.18m, kept.Balance);
        Assert.Equal(1240.18m, kept.BalanceInUse);
        Assert.Equal(new DateOnly(2026, 10, 2), kept.BalanceAsOf);
        Assert.Equal(1240.18m, kept.SyncedBalance);
        Assert.Equal(900m, await AccountBalanceAsync(dbContext, accountId));

        var edited = Card(accountId);
        edited.Balance = 1m;
        edited.Apr = 9m;
        var saved = await service.UpdateAsync(debt.Id, edited);
        Assert.Equal(1240.18m, saved.Balance);
        Assert.Equal(DebtFieldSource.Override, saved.BalanceSource);
        Assert.Equal(9m, saved.Apr);

        var synced = await service.ClearOverrideAsync(debt.Id, DebtSyncedField.Balance);
        Assert.Equal(DebtFieldSource.Synced, synced.BalanceSource);
        Assert.Equal(1240.18m, synced.Balance);
        Assert.Equal(new DateOnly(2026, 10, 2), synced.BalanceAsOf);
        Assert.Equal(1240.18m, synced.BalanceInUse);
        Assert.Equal(new DateOnly(2026, 10, 5), synced.BalanceInUseAsOf);

        var own = await service.SetOverrideAsync(
            debt.Id,
            DebtSyncedField.Balance,
            new SetDebtBalanceOverrideDto
            {
                Balance = 500m,
                BalanceAsOf = new DateOnly(2026, 10, 3)
            });
        var stopped = await service.StopFollowingAsync(own.Id);
        Assert.False(stopped.Following);
        Assert.Equal(500m, stopped.Balance);
        Assert.Equal(new DateOnly(2026, 10, 3), stopped.BalanceAsOf);
        Assert.Equal(DebtFieldSource.Manual, stopped.BalanceSource);
    }

    [Fact]
    public async Task Override_UsesTodayWhenTheDateIsOmittedAndRejectsADebtThatIsNotFollowing()
    {
        await using var dbContext = CreateDbContext();
        var householdId = await CreateHouseholdAsync(dbContext, "user_owner");
        var accountId = await AddConnectedAccountAsync(dbContext, householdId, 900m, snapshot: 1240.18m);
        var service = CreateService(dbContext, Bind(householdId));
        var debt = await service.CreateAsync(Card(accountId));

        var early = await Assert.ThrowsAsync<BadRequestException>(
            () => service.SetOverrideAsync(
                debt.Id,
                DebtSyncedField.Balance,
                new SetDebtBalanceOverrideDto { Balance = 10m }));
        Assert.Equal("This debt is not following an account.", early.Message);

        await service.FollowAccountAsync(
            debt.Id,
            new FollowDebtAccountDto { AccountId = accountId });
        var updated = await service.SetOverrideAsync(
            debt.Id,
            DebtSyncedField.Balance,
            new SetDebtBalanceOverrideDto { Balance = 10m });
        Assert.Equal(DebtFieldSource.Override, updated.BalanceSource);
        Assert.Equal(10m, updated.BalanceInUse);
        Assert.Equal(new DateOnly(2026, 10, 5), updated.BalanceAsOf);
        Assert.Equal(1240.18m, updated.SyncedBalance);

        var blank = await Assert.ThrowsAsync<BadRequestException>(
            () => service.SetOverrideAsync(
                debt.Id,
                DebtSyncedField.Balance,
                new SetDebtBalanceOverrideDto()));
        Assert.Equal("Enter the balance.", blank.Message);
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

    private static async Task<Guid> AddConnectedAccountAsync(
        CarduiDBContext dbContext,
        Guid householdId,
        decimal currentBalance,
        decimal snapshot)
    {
        var item = new PlaidItem
        {
            Id = Guid.NewGuid(),
            HouseholdId = householdId,
            PlaidItemId = "item-1",
            AccessToken = "stored-token",
            LastSyncCompletedAt = Now,
            CreatedAt = Now,
            UpdatedAt = Now
        };
        dbContext.PlaidItems.Add(item);
        var account = new Account
        {
            Id = Guid.NewGuid(),
            HouseholdId = householdId,
            PlaidItemId = item.Id,
            PlaidAccountId = "account-1",
            Source = FinancialRecordSource.Plaid,
            Provenance = FinancialRecordProvenance.PlaidSync,
            Name = "Visa",
            Mask = "4821",
            Type = AccountTypes.Credit,
            IsoCurrencyCode = "USD",
            IsActive = true,
            CurrentBalance = currentBalance,
            CreatedAt = Now,
            UpdatedAt = Now
        };
        dbContext.Accounts.Add(account);
        await dbContext.SaveChangesAsync();
        await AddSnapshotAsync(dbContext, account.Id, new DateOnly(2026, 10, 5), snapshot, "USD");
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
