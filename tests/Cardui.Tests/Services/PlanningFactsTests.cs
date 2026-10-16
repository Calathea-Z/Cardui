using Cardui.Api.Data;
using Cardui.Api.Domain.Accounts;
using Cardui.Api.Domain.Income;
using Cardui.Api.Domain.Planning;
using Cardui.Api.Dtos.Account;
using Cardui.Api.Dtos.Debts;
using Cardui.Api.Dtos.Income;
using Cardui.Api.Dtos.Obligations;
using Cardui.Api.Dtos.Planning;
using Cardui.Api.Dtos.Savings;
using Cardui.Api.Models;
using Cardui.Api.Security;
using Cardui.Api.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Cardui.Tests.Services;

public class PlanningFactsTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 16, 18, 0, 0, TimeSpan.Zero);

    private static readonly DateOnly Today = new(2026, 10, 16);

    [Fact]
    public async Task Describe_ReturnsNothingForAnEmptyHouseholdAndWritesNothing()
    {
        await using var db = CreateDbContext();
        var household = await SeedAsync(db);

        var facts = await household.Facts.DescribeAsync();

        Assert.Empty(facts);
        Assert.Equal(0, await db.Accounts.CountAsync());
        Assert.Equal(0, await db.IncomeSources.CountAsync());
        Assert.Equal(0, await db.Obligations.CountAsync());
        Assert.Equal(0, await db.Debts.CountAsync());
        Assert.Equal(0, await db.SavingsGoals.CountAsync());
        Assert.Equal(0, await db.Conversations.CountAsync());
        Assert.Equal(0, await db.ObligationSuggestionDismissals.CountAsync());
    }

    [Fact]
    public async Task Describe_LabelsWhatThePersonEntered()
    {
        await using var db = CreateDbContext();
        var household = await SeedAsync(db);
        var checking = await household.Accounts.CreateManualAccountAsync(CashAccount("Checking", 500m));
        await household.Incomes.CreateAsync(Pay("Gift", 100m, IncomeCadence.Irregular, new DateOnly(2026, 12, 1)));
        var paycheck = await household.Incomes.CreateAsync(Pay("Paycheck", 2000m, IncomeCadence.Biweekly, new DateOnly(2026, 10, 20)));
        var rent = await household.Bills.CreateAsync(Bill("Rent", 900m));
        var card = await household.Debts.CreateAsync(new UpsertDebtDto
        {
            Name = "Card",
            Kind = DebtKind.Revolving,
            Balance = 800m,
            BalanceAsOf = Today,
            MinimumPayment = 0m,
            NextDueDate = new DateOnly(2026, 10, 20)
        });
        var living = await household.Savings.CreateAsync(new UpsertSavingsGoalDto
        {
            Kind = SavingsGoalKind.Operating,
            MonthlyAmount = 400m,
            ReadyDay = 1
        });
        var conversation = await household.Conversations.StartAsync();
        await household.Conversations.SetPreferenceAsync(conversation.Id, "Preference", "Keep some cash");
        await household.Conversations.SetPreferenceAsync(conversation.Id, "Question:CashOnHand", "unknown");

        var facts = await household.Facts.DescribeAsync();

        var cash = One(facts, PlanningFactKind.CashBalance, "Checking");
        Assert.Equal(checking.Id, cash.SubjectId);
        Assert.Equal(PlanningFactBasis.UserStatement, cash.Basis);
        Assert.Equal(500m, cash.Amount);
        Assert.False(cash.ConfirmedObligation);

        var gift = One(facts, PlanningFactKind.IncomePayment, "Gift");
        Assert.Equal(100m, gift.Amount);
        Assert.Equal(new DateOnly(2026, 12, 1), gift.AsOf);
        Assert.DoesNotContain(facts, fact =>
            fact.Kind == PlanningFactKind.IncomeMonthlyAverage && fact.Name == "Gift");

        var payment = One(facts, PlanningFactKind.IncomePayment, "Paycheck");
        var average = One(facts, PlanningFactKind.IncomeMonthlyAverage, "Paycheck");
        Assert.Equal(paycheck.Id, payment.SubjectId);
        Assert.Equal(paycheck.Id, average.SubjectId);
        Assert.Equal(2000m, payment.Amount);
        Assert.Equal(new DateOnly(2026, 10, 20), payment.AsOf);
        Assert.Equal(PlanningFactBasis.UserStatement, payment.Basis);
        Assert.Equal(PaycheckSchedule.AverageMonthlyAmount(2000m, IncomeCadence.Biweekly), average.Amount);
        Assert.Equal(PlanningFactBasis.Estimate, average.Basis);
        Assert.Null(average.AsOf);
        Assert.False(average.ConfirmedObligation);

        var bill = One(facts, PlanningFactKind.Bill, "Rent");
        Assert.Equal(rent.Id, bill.SubjectId);
        Assert.Equal(PlanningFactBasis.UserStatement, bill.Basis);
        Assert.True(bill.ConfirmedObligation);
        Assert.Equal(900m, bill.Amount);

        var balance = One(facts, PlanningFactKind.DebtBalance, "Card");
        var minimum = One(facts, PlanningFactKind.DebtMinimum, "Card");
        var due = One(facts, PlanningFactKind.DebtDueDate, "Card");
        Assert.Equal(card.Id, balance.SubjectId);
        Assert.Equal(800m, balance.Amount);
        Assert.Equal(PlanningFactBasis.UserStatement, balance.Basis);
        Assert.Equal(0m, minimum.Amount);
        Assert.False(minimum.ConfirmedObligation);
        Assert.Equal(new DateOnly(2026, 10, 20), due.AsOf);
        Assert.Null(due.Amount);
        Assert.DoesNotContain(facts, fact => fact.Kind == PlanningFactKind.DebtApr);

        var spending = One(facts, PlanningFactKind.LivingSpending, SavingsGoal.OperatingName);
        Assert.Equal(living.Id, spending.SubjectId);
        Assert.Equal(400m, spending.Amount);
        Assert.Equal(PlanningFactBasis.UserStatement, spending.Basis);
        Assert.False(spending.ConfirmedObligation);

        var preference = One(facts, PlanningFactKind.ConversationNote, "Preference");
        var unknown = One(facts, PlanningFactKind.ConversationNote, "Question:CashOnHand");
        Assert.Equal("Keep some cash", preference.Detail);
        Assert.Equal("unknown", unknown.Detail);
        Assert.Null(preference.Amount);
        Assert.Null(unknown.Amount);
        Assert.Equal(PlanningFactBasis.UserStatement, unknown.Basis);

        Assert.Equal(1, await db.Accounts.CountAsync());
        Assert.Equal(2, await db.IncomeSources.CountAsync());
        Assert.Equal(1, await db.Obligations.CountAsync());
        Assert.Equal(1, await db.Debts.CountAsync(debt => debt.MinimumPayment == 0m && debt.Apr == null));
        Assert.Equal(1, await db.SavingsGoals.CountAsync());
        Assert.Equal(0, await db.ObligationSuggestionDismissals.CountAsync());
    }

    [Fact]
    public async Task Describe_KeepsAnInferredExpenseOffTheBillListUntilThePersonSavesIt()
    {
        await using var db = CreateDbContext();
        var household = await SeedAsync(db);
        var checking = await household.Accounts.CreateManualAccountAsync(CashAccount("Checking", 500m));
        await AddMonthlyNetflixAsync(db, checking.Id);

        var noticed = await household.Facts.DescribeAsync();

        var suggestion = One(noticed, PlanningFactKind.RecurringExpense, "Netflix");
        Assert.Equal(PlanningFactBasis.UnverifiedSuggestion, suggestion.Basis);
        Assert.Equal("netflix", suggestion.Detail);
        Assert.Equal(15.99m, suggestion.Amount);
        Assert.Equal(checking.Id, suggestion.RelatedId);
        Assert.Equal(new DateOnly(2026, 11, 1), suggestion.AsOf);
        Assert.False(suggestion.ConfirmedObligation);
        Assert.DoesNotContain(noticed, fact => fact.Kind == PlanningFactKind.Bill);
        Assert.Equal(0, await db.Obligations.CountAsync());

        await household.Bills.CreateAsync(new UpsertObligationDto
        {
            Name = "Netflix",
            Amount = 15.99m,
            Cadence = ObligationCadence.Monthly,
            NextDueDate = new DateOnly(2026, 11, 1),
            AccountId = checking.Id,
            Flexibility = ObligationFlexibility.Flexible,
            SuggestionKey = suggestion.Detail
        });
        var saved = await household.Facts.DescribeAsync();

        var bill = One(saved, PlanningFactKind.Bill, "Netflix");
        Assert.Equal(PlanningFactBasis.UserStatement, bill.Basis);
        Assert.True(bill.ConfirmedObligation);
        Assert.DoesNotContain(saved, fact => fact.Kind == PlanningFactKind.RecurringExpense);
        Assert.Equal(1, await db.Obligations.CountAsync());
        Assert.Equal(0, await db.ObligationSuggestionDismissals.CountAsync());
    }

    [Fact]
    public async Task Describe_LabelsImportedCashAndLeavesAMatchUnfollowed()
    {
        await using var db = CreateDbContext();
        var household = await SeedAsync(db);
        await household.Accounts.CreateManualAccountAsync(CashAccount("Checking", 100m));
        await AddCashAccountAsync(db, household.HouseholdId, "Bank", 800m, FinancialRecordSource.Plaid);
        await AddCashAccountAsync(db, household.HouseholdId, "Imported", 40m, FinancialRecordSource.Csv);
        var visa = await AddConnectedCardAsync(db, household.HouseholdId);
        var debt = await household.Debts.CreateAsync(new UpsertDebtDto
        {
            Name = "Visa 4821",
            Kind = DebtKind.Revolving,
            Balance = 500m,
            BalanceAsOf = Today
        });

        var facts = await household.Facts.DescribeAsync();

        Assert.Equal(PlanningFactBasis.ImportedFact, One(facts, PlanningFactKind.CashBalance, "Bank").Basis);
        Assert.Equal(800m, One(facts, PlanningFactKind.CashBalance, "Bank").Amount);
        Assert.Equal(PlanningFactBasis.UserStatement, One(facts, PlanningFactKind.CashBalance, "Checking").Basis);
        Assert.Equal(PlanningFactBasis.ImportedFact, One(facts, PlanningFactKind.CashBalance, "Imported").Basis);
        Assert.DoesNotContain(facts, fact => fact.Kind == PlanningFactKind.CashBalance && fact.Name == "Visa");

        var balance = One(facts, PlanningFactKind.DebtBalance, "Visa 4821");
        Assert.Equal(500m, balance.Amount);
        Assert.Equal(PlanningFactBasis.UserStatement, balance.Basis);
        var match = One(facts, PlanningFactKind.DebtMatch, "Visa 4821");
        Assert.Equal(debt.Id, match.SubjectId);
        Assert.Equal(visa, match.RelatedId);
        Assert.Equal("Visa", match.Detail);
        Assert.Null(match.Amount);
        Assert.Equal(PlanningFactBasis.UnverifiedSuggestion, match.Basis);
        Assert.False(match.ConfirmedObligation);
        Assert.Equal(0, await db.Obligations.CountAsync());
        Assert.Equal(0, await db.Debts.CountAsync(row => row.AccountFollowedSince != null));

        await household.Debts.FollowAccountAsync(debt.Id, new FollowDebtAccountDto { AccountId = visa });
        var followed = await household.Facts.DescribeAsync();

        var synced = One(followed, PlanningFactKind.DebtBalance, "Visa 4821");
        Assert.Equal(640m, synced.Amount);
        Assert.Equal(Today, synced.AsOf);
        Assert.Equal(PlanningFactBasis.ImportedFact, synced.Basis);
        Assert.False(synced.ConfirmedObligation);
        var limit = One(followed, PlanningFactKind.DebtCreditLimit, "Visa 4821");
        Assert.Equal(2000m, limit.Amount);
        Assert.Equal(PlanningFactBasis.ImportedFact, limit.Basis);
        Assert.DoesNotContain(followed, fact => fact.Kind == PlanningFactKind.DebtMatch);
        Assert.Equal(0, await db.Obligations.CountAsync());
        Assert.Equal(1, await db.Debts.CountAsync(row => row.AccountFollowedSince != null));
    }

    [Fact]
    public async Task Describe_StaysInsideTheHousehold()
    {
        await using var db = CreateDbContext();
        var household = await SeedAsync(db);
        await household.Bills.CreateAsync(Bill("Rent", 900m));
        var otherScope = new HouseholdScope();
        otherScope.Bind(household.OtherHouseholdId);
        var other = Services(db, otherScope, household.Time);
        var otherAccount = await other.Accounts.CreateManualAccountAsync(CashAccount("Other checking", 9999m));
        await AddMonthlyChargeAsync(db, otherAccount.Id, "Spotify");
        await other.Bills.CreateAsync(Bill("Other rent", 100m));

        var facts = await household.Facts.DescribeAsync();

        var bill = Assert.Single(facts);
        Assert.Equal(PlanningFactKind.Bill, bill.Kind);
        Assert.Equal("Rent", bill.Name);
        Assert.Equal(2, await db.Obligations.CountAsync());
    }

    [Fact]
    public async Task Describe_DropsANoteWhenTheConversationIsClosed()
    {
        await using var db = CreateDbContext();
        var household = await SeedAsync(db);
        var conversation = await household.Conversations.StartAsync();
        await household.Conversations.SetPreferenceAsync(conversation.Id, "Question:CashOnHand", "unknown");

        var open = await household.Facts.DescribeAsync();
        await household.Conversations.CloseAsync(conversation.Id);
        var closed = await household.Facts.DescribeAsync();

        Assert.Equal("unknown", One(open, PlanningFactKind.ConversationNote, "Question:CashOnHand").Detail);
        Assert.Empty(closed);
        Assert.Equal(ConversationStatus.Closed, (await db.Conversations.SingleAsync()).Status);
        Assert.Equal(1, await db.ConversationPreferences.CountAsync());
        Assert.Equal(0, await db.Accounts.CountAsync());
        Assert.Equal(0, await db.Obligations.CountAsync());
    }

    [Fact]
    public async Task Describe_RequiresAHousehold()
    {
        await using var db = CreateDbContext();
        var facts = CreateFacts(db, new HouseholdScope(), new FakeTimeProvider(Now));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => facts.DescribeAsync());

        Assert.Equal("A household scope is required.", error.Message);
    }

    #region Private Methods

    private static PlanningFactDto One(
        IReadOnlyList<PlanningFactDto> facts,
        PlanningFactKind kind,
        string name)
    {
        return Assert.Single(facts, fact => fact.Kind == kind && fact.Name == name);
    }

    private static async Task<HouseholdFacts> SeedAsync(CarduiDBContext db)
    {
        var time = new FakeTimeProvider(Now);
        var households = new HouseholdsService(db, time);
        var home = await households.GetOrCreateForOwnerAsync("owner_home");
        var other = await households.GetOrCreateForOwnerAsync("owner_other");
        var scope = new HouseholdScope();
        scope.Bind(home.Id);
        return new HouseholdFacts(
            CreateFacts(db, scope, time),
            Services(db, scope, time),
            other.Id,
            home.Id,
            time);
    }

    private static PlanningFacts CreateFacts(
        CarduiDBContext db,
        HouseholdScope scope,
        TimeProvider time)
    {
        var services = Services(db, scope, time);
        return new PlanningFacts(
            services.Accounts,
            services.Incomes,
            services.Bills,
            services.Debts,
            services.Living,
            services.Conversations,
            scope);
    }

    private static HouseholdServices Services(
        CarduiDBContext db,
        HouseholdScope scope,
        TimeProvider time)
    {
        var incomes = new IncomeSourcesService(db, time, scope);
        var obligations = new ObligationsService(db, time, scope);
        var debts = new DebtsService(db, time, scope);
        var savings = new SavingsGoalsService(db, time, scope);
        return new HouseholdServices(
            new AccountsService(db, time, scope),
            incomes,
            obligations,
            debts,
            savings,
            new LivingService(db, incomes, obligations, debts, savings, time, scope),
            new ConversationService(db, time, scope));
    }

    private static CreateManualAccountDto CashAccount(string name, decimal balance)
    {
        return new CreateManualAccountDto
        {
            Name = name,
            Type = AccountTypes.Depository,
            IsoCurrencyCode = "USD",
            OpeningBalance = balance,
            OpeningBalanceDate = new DateOnly(2026, 10, 1)
        };
    }

    private static UpsertIncomeSourceDto Pay(
        string name,
        decimal amount,
        IncomeCadence cadence,
        DateOnly nextPaymentDate)
    {
        return new UpsertIncomeSourceDto
        {
            Name = name,
            TakeHomeAmount = amount,
            Cadence = cadence,
            NextPaymentDate = nextPaymentDate,
            Reliability = cadence == IncomeCadence.Irregular
                ? IncomeReliability.Uncertain
                : IncomeReliability.Steady
        };
    }

    private static UpsertObligationDto Bill(string name, decimal amount)
    {
        return new UpsertObligationDto
        {
            Name = name,
            Amount = amount,
            Cadence = ObligationCadence.Monthly,
            NextDueDate = new DateOnly(2026, 10, 25),
            Flexibility = ObligationFlexibility.Essential
        };
    }

    private static async Task AddMonthlyNetflixAsync(CarduiDBContext db, Guid accountId)
    {
        await AddChargeAsync(db, accountId, new DateOnly(2026, 8, 1), "aug", "Netflix");
        await AddChargeAsync(db, accountId, new DateOnly(2026, 9, 1), "sep", "Netflix");
        await AddChargeAsync(db, accountId, new DateOnly(2026, 10, 1), "oct", "Netflix");
    }

    private static async Task AddMonthlyChargeAsync(CarduiDBContext db, Guid accountId, string merchant)
    {
        await AddChargeAsync(db, accountId, new DateOnly(2026, 8, 1), merchant + "-aug", merchant);
        await AddChargeAsync(db, accountId, new DateOnly(2026, 9, 1), merchant + "-sep", merchant);
        await AddChargeAsync(db, accountId, new DateOnly(2026, 10, 1), merchant + "-oct", merchant);
    }

    private static async Task AddChargeAsync(
        CarduiDBContext db,
        Guid accountId,
        DateOnly date,
        string id,
        string merchant)
    {
        db.Transactions.Add(new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            PlaidTransactionId = "transaction-" + id,
            Source = FinancialRecordSource.Manual,
            Provenance = FinancialRecordProvenance.ManualEntry,
            Date = date,
            Name = "Card purchase",
            MerchantName = merchant,
            Amount = 15.99m,
            IsoCurrencyCode = "USD",
            CreatedAt = Now,
            UpdatedAt = Now
        });
        await db.SaveChangesAsync();
    }

    private static async Task AddCashAccountAsync(
        CarduiDBContext db,
        Guid householdId,
        string name,
        decimal balance,
        FinancialRecordSource source)
    {
        Guid? itemId = null;
        if (source == FinancialRecordSource.Plaid)
        {
            itemId = await AddPlaidItemAsync(db, householdId);
        }

        db.Accounts.Add(new Account
        {
            Id = Guid.NewGuid(),
            HouseholdId = householdId,
            PlaidItemId = itemId,
            PlaidAccountId = itemId is null ? null : Guid.NewGuid().ToString("N"),
            Source = source,
            Provenance = source == FinancialRecordSource.Csv
                ? FinancialRecordProvenance.CsvImport
                : FinancialRecordProvenance.PlaidSync,
            Name = name,
            Type = AccountTypes.Depository,
            IsoCurrencyCode = "USD",
            IsActive = true,
            CurrentBalance = balance,
            CreatedAt = Now,
            UpdatedAt = Now
        });
        await db.SaveChangesAsync();
    }

    private static async Task<Guid> AddConnectedCardAsync(CarduiDBContext db, Guid householdId)
    {
        var itemId = await AddPlaidItemAsync(db, householdId);
        var account = new Account
        {
            Id = Guid.NewGuid(),
            HouseholdId = householdId,
            PlaidItemId = itemId,
            PlaidAccountId = Guid.NewGuid().ToString("N"),
            Source = FinancialRecordSource.Plaid,
            Provenance = FinancialRecordProvenance.PlaidSync,
            Name = "Visa",
            Mask = "4821",
            Type = AccountTypes.Credit,
            IsoCurrencyCode = "USD",
            IsActive = true,
            CurrentBalance = 640m,
            CreditLimit = 2000m,
            CreatedAt = Now,
            UpdatedAt = Now
        };
        db.Accounts.Add(account);
        db.AccountBalanceSnapshots.Add(new AccountBalanceSnapshot
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            Date = Today,
            CurrentBalance = 640m,
            IsoCurrencyCode = "USD",
            CreatedAt = Now
        });
        await db.SaveChangesAsync();
        return account.Id;
    }

    private static async Task<Guid> AddPlaidItemAsync(CarduiDBContext db, Guid householdId)
    {
        var item = new PlaidItem
        {
            Id = Guid.NewGuid(),
            HouseholdId = householdId,
            PlaidItemId = Guid.NewGuid().ToString("N"),
            AccessToken = "stored-token",
            LastSyncCompletedAt = Now,
            CreatedAt = Now,
            UpdatedAt = Now
        };
        db.PlaidItems.Add(item);
        await db.SaveChangesAsync();
        return item.Id;
    }

    private static CarduiDBContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CarduiDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CarduiDBContext(options);
    }

    private sealed record HouseholdFacts(
        PlanningFacts Facts,
        HouseholdServices Services,
        Guid OtherHouseholdId,
        Guid HouseholdId,
        FakeTimeProvider Time)
    {
        public AccountsService Accounts => Services.Accounts;

        public IncomeSourcesService Incomes => Services.Incomes;

        public ObligationsService Bills => Services.Bills;

        public DebtsService Debts => Services.Debts;

        public SavingsGoalsService Savings => Services.Savings;

        public ConversationService Conversations => Services.Conversations;
    }

    private sealed record HouseholdServices(
        AccountsService Accounts,
        IncomeSourcesService Incomes,
        ObligationsService Bills,
        DebtsService Debts,
        SavingsGoalsService Savings,
        LivingService Living,
        ConversationService Conversations);

    #endregion
}
