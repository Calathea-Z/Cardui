using Cardui.Api.Domain.Categories;
using Cardui.Api.Domain.Obligations;
using Cardui.Api.Models;
using Xunit;

namespace Cardui.Tests.Domain;

public class RecurringSuggestionsTests
{
    private static readonly DateOnly Today = new(2026, 10, 5);

    private static readonly Guid AccountId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public void Find_SuggestsOneMonthlyPayment()
    {
        var suggestion = Assert.Single(Find(
            Charge(new DateOnly(2026, 8, 1)),
            Charge(new DateOnly(2026, 9, 1)),
            Charge(new DateOnly(2026, 10, 1), name: "POS", merchant: " Netflix  ")));

        Assert.Equal("netflix", suggestion.Key);
        Assert.Equal("Netflix", suggestion.Name);
        Assert.Equal(15.99m, suggestion.Amount);
        Assert.Equal(ObligationCadence.Monthly, suggestion.Cadence);
        Assert.Equal(new DateOnly(2026, 11, 1), suggestion.NextDueDate);
        Assert.Equal(AccountId, suggestion.AccountId);
        Assert.Equal("Checking", suggestion.AccountName);
    }

    [Fact]
    public void Find_KeepsTheLargerChargeWhenTwoLandOnTheSameDay()
    {
        var suggestion = Assert.Single(Find(
            Charge(new DateOnly(2026, 8, 1)),
            Charge(new DateOnly(2026, 8, 1), amount: 1m),
            Charge(new DateOnly(2026, 9, 1)),
            Charge(new DateOnly(2026, 10, 1))));

        Assert.Equal(15.99m, suggestion.Amount);
        Assert.Equal(ObligationCadence.Monthly, suggestion.Cadence);
    }

    [Fact]
    public void Find_SuggestsWeeklyBiweeklyQuarterlyAndYearly()
    {
        var weekly = Assert.Single(Find(
            Charge(new DateOnly(2026, 9, 14), merchant: "Weekly"),
            Charge(new DateOnly(2026, 9, 21), merchant: "Weekly"),
            Charge(new DateOnly(2026, 9, 28), merchant: "Weekly"),
            Charge(new DateOnly(2026, 10, 5), merchant: "Weekly")));
        Assert.Equal(ObligationCadence.Weekly, weekly.Cadence);
        Assert.Equal(new DateOnly(2026, 10, 12), weekly.NextDueDate);

        var biweekly = Assert.Single(Find(
            Charge(new DateOnly(2026, 9, 4), merchant: "Biweekly"),
            Charge(new DateOnly(2026, 9, 18), merchant: "Biweekly"),
            Charge(new DateOnly(2026, 10, 2), merchant: "Biweekly")));
        Assert.Equal(ObligationCadence.Biweekly, biweekly.Cadence);
        Assert.Equal(new DateOnly(2026, 10, 16), biweekly.NextDueDate);

        var quarterly = Assert.Single(Find(
            Charge(new DateOnly(2026, 1, 5), merchant: "Quarterly"),
            Charge(new DateOnly(2026, 4, 5), merchant: "Quarterly"),
            Charge(new DateOnly(2026, 7, 5), merchant: "Quarterly")));
        Assert.Equal(ObligationCadence.Quarterly, quarterly.Cadence);
        Assert.Equal(new DateOnly(2026, 10, 5), quarterly.NextDueDate);

        var yearly = Assert.Single(Find(
            Charge(new DateOnly(2024, 10, 1), merchant: "Yearly"),
            Charge(new DateOnly(2025, 10, 1), merchant: "Yearly"),
            Charge(new DateOnly(2026, 10, 1), merchant: "Yearly")));
        Assert.Equal(ObligationCadence.Yearly, yearly.Cadence);
        Assert.Equal(new DateOnly(2027, 10, 1), yearly.NextDueDate);
    }

    [Fact]
    public void Find_TreatsAStretchedGapAsTwiceAMonth()
    {
        var suggestion = Assert.Single(Find(
            Charge(new DateOnly(2026, 8, 15), merchant: "Daycare"),
            Charge(new DateOnly(2026, 9, 1), merchant: "Daycare"),
            Charge(new DateOnly(2026, 9, 15), merchant: "Daycare")));

        Assert.Equal(ObligationCadence.Semimonthly, suggestion.Cadence);
        Assert.Equal(new DateOnly(2026, 10, 1), suggestion.NextDueDate);
    }

    [Fact]
    public void Find_TreatsASteadyFourteenDayGapAsEveryTwoWeeks()
    {
        var suggestion = Assert.Single(FindOn(
            new DateOnly(2026, 3, 5),
            Charge(new DateOnly(2026, 2, 1), merchant: "Gym"),
            Charge(new DateOnly(2026, 2, 15), merchant: "Gym"),
            Charge(new DateOnly(2026, 3, 1), merchant: "Gym")));

        Assert.Equal(ObligationCadence.Biweekly, suggestion.Cadence);
        Assert.Equal(new DateOnly(2026, 3, 15), suggestion.NextDueDate);
    }

    [Fact]
    public void Find_SkipsShortUnevenStoppedOrExcludedCharges()
    {
        Assert.Empty(Find(
            Charge(new DateOnly(2026, 9, 1)),
            Charge(new DateOnly(2026, 10, 1))));

        Assert.Empty(Find(
            Charge(new DateOnly(2026, 8, 1), amount: 40m, merchant: "Market"),
            Charge(new DateOnly(2026, 9, 1), amount: 90m, merchant: "Market"),
            Charge(new DateOnly(2026, 10, 1), amount: 20m, merchant: "Market")));

        Assert.Empty(FindOn(
            new DateOnly(2026, 11, 5),
            Charge(new DateOnly(2026, 8, 1), merchant: "Gap"),
            Charge(new DateOnly(2026, 9, 1), merchant: "Gap"),
            Charge(new DateOnly(2026, 11, 1), merchant: "Gap")));

        Assert.Empty(Find(
            Charge(new DateOnly(2026, 1, 1), merchant: "Old"),
            Charge(new DateOnly(2026, 2, 1), merchant: "Old"),
            Charge(new DateOnly(2026, 3, 1), merchant: "Old")));

        Assert.Empty(Find(
            Charge(new DateOnly(2026, 8, 1), pending: true),
            Charge(new DateOnly(2026, 9, 1), pending: true),
            Charge(new DateOnly(2026, 10, 1), pending: true)));

        Assert.Empty(Find(
            Charge(new DateOnly(2026, 8, 1), archived: true),
            Charge(new DateOnly(2026, 9, 1), archived: true),
            Charge(new DateOnly(2026, 10, 1), archived: true)));

        Assert.Empty(Find(
            Charge(new DateOnly(2026, 8, 1), provenance: FinancialRecordProvenance.BalanceReconciliation),
            Charge(new DateOnly(2026, 9, 1), provenance: FinancialRecordProvenance.BalanceReconciliation),
            Charge(new DateOnly(2026, 10, 1), provenance: FinancialRecordProvenance.BalanceReconciliation)));

        Assert.Empty(Find(
            Charge(new DateOnly(2026, 8, 1), groupKey: SystemGroupKeys.Transfers),
            Charge(new DateOnly(2026, 9, 1), groupKey: SystemGroupKeys.Transfers),
            Charge(new DateOnly(2026, 10, 1), groupKey: SystemGroupKeys.Transfers)));

        Assert.Empty(Find(
            Charge(new DateOnly(2026, 8, 1), categoryKey: SystemCategoryKeys.Income),
            Charge(new DateOnly(2026, 9, 1), categoryKey: SystemCategoryKeys.Income),
            Charge(new DateOnly(2026, 10, 1), categoryKey: SystemCategoryKeys.Income)));

        Assert.Empty(Find(
            Charge(new DateOnly(2026, 8, 1), amount: -15.99m),
            Charge(new DateOnly(2026, 9, 1), amount: -15.99m),
            Charge(new DateOnly(2026, 10, 1), amount: -15.99m)));

        Assert.Empty(Find(
            Charge(new DateOnly(2026, 8, 1), currency: "EUR"),
            Charge(new DateOnly(2026, 9, 1), currency: "EUR"),
            Charge(new DateOnly(2026, 10, 1), currency: "EUR")));
    }

    [Fact]
    public void Find_LeavesTheAccountEmptyWhenChargesUseMoreThanOne()
    {
        var otherAccountId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var suggestion = Assert.Single(Find(
            Charge(new DateOnly(2026, 8, 1)),
            Charge(new DateOnly(2026, 9, 1), accountId: otherAccountId),
            Charge(new DateOnly(2026, 10, 1))));

        Assert.Null(suggestion.AccountId);
        Assert.Null(suggestion.AccountName);
    }

    [Fact]
    public void Find_HidesAPatternThatIsAlreadyABillOrWasDismissed()
    {
        var charges = new[]
        {
            Charge(new DateOnly(2026, 8, 1)),
            Charge(new DateOnly(2026, 9, 1)),
            Charge(new DateOnly(2026, 10, 1))
        };

        Assert.Empty(RecurringSuggestions.Find(charges, ["netflix"], [], [], "USD", Today));
        Assert.Empty(RecurringSuggestions.Find(charges, ["Streaming"], ["Netflix"], [], "USD", Today));
        Assert.Empty(RecurringSuggestions.Find(charges, [], [], [" Netflix "], "USD", Today));
    }

    private static IReadOnlyList<RecurringSuggestion> Find(
        params RecurringSuggestionCharge[] charges)
    {
        return FindOn(Today, charges);
    }

    private static IReadOnlyList<RecurringSuggestion> FindOn(
        DateOnly today,
        params RecurringSuggestionCharge[] charges)
    {
        return RecurringSuggestions.Find(
            charges,
            [],
            [],
            [],
            "USD",
            today);
    }

    private static RecurringSuggestionCharge Charge(
        DateOnly date,
        decimal amount = 15.99m,
        string merchant = "Netflix",
        string name = "Card purchase",
        Guid? accountId = null,
        string? currency = "USD",
        bool pending = false,
        bool archived = false,
        string? categoryKey = null,
        string? groupKey = null,
        FinancialRecordProvenance provenance = FinancialRecordProvenance.PlaidSync)
    {
        return new RecurringSuggestionCharge(
            date,
            amount,
            name,
            merchant,
            accountId ?? AccountId,
            "Checking",
            currency,
            pending,
            archived,
            categoryKey,
            groupKey,
            provenance);
    }
}
