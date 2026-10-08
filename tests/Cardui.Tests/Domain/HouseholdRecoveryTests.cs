using Cardui.Api.Domain.Recovery;
using Cardui.Api.Models;
using Xunit;

namespace Cardui.Tests.Domain;

public class HouseholdRecoveryTests
{
    private static readonly Guid CardId = Guid.Parse("60000000-0000-0000-0000-000000000001");
    private static readonly Guid StoreId = Guid.Parse("60000000-0000-0000-0000-000000000002");
    private static readonly Guid BlankId = Guid.Parse("60000000-0000-0000-0000-000000000003");
    private static readonly Guid OtherBlankId = Guid.Parse("60000000-0000-0000-0000-000000000004");
    private static readonly DateOnly Today = new(2026, 1, 1);

    [Fact]
    public void Prepare_UsesTheBalanceInUseAndNoExtra()
    {
        var prepared = HouseholdRecovery.Prepare(
            "USD",
            Today,
            [
                Debt(
                    CardId,
                    "Card",
                    balance: 180m,
                    apr: 19.99m,
                    minimum: 40m,
                    due: new DateOnly(2026, 1, 15),
                    limit: 1000m)
            ],
            0m);

        var input = prepared.Rollover;
        var debt = Assert.Single(input.Debts);
        Assert.Equal(180m, debt.Terms.Balance);
        Assert.Equal(0m, debt.Terms.ExtraPayment);
        Assert.Equal(1000m, debt.CreditLimit);
        Assert.Equal(0m, input.MonthlyExtra);
        Assert.Equal(0m, input.ReclaimAmount);
        Assert.Empty(input.Order);
        Assert.Empty(prepared.MissingBalance);
    }

    [Fact]
    public void Prepare_LeavesAnotherCurrencyForTheExclusionRule()
    {
        var input = HouseholdRecovery.Prepare(
            "USD",
            Today,
            [
                Debt(CardId, "Card", balance: 100m, apr: 12m, minimum: 25m, due: new DateOnly(2026, 1, 15)),
                Debt(
                    StoreId,
                    "Store",
                    balance: 50m,
                    apr: 0m,
                    minimum: 10m,
                    due: new DateOnly(2026, 1, 15),
                    currency: "CAD")
            ],
            0m).Rollover;

        Assert.Contains(input.Debts, debt => debt.Terms.DebtId == StoreId && debt.Terms.Currency == "CAD");

        var recovery = CashFlowRecovery.Track(PayoffRollover.Compare(input));
        Assert.Equal(["CAD"], recovery.ExcludedCurrencies);
        Assert.DoesNotContain(recovery.Rollover.Steps, step => step.DebtId == StoreId);
    }

    [Fact]
    public void Prepare_KeepsAMissingRateMinimumOrDueDateUnknown()
    {
        var input = HouseholdRecovery.Prepare(
            "USD",
            Today,
            [Debt(CardId, "Card", balance: 100m, apr: null, minimum: null, due: null)],
            0m).Rollover;

        var debt = Assert.Single(input.Debts);
        Assert.Null(debt.Terms.Apr);
        Assert.Null(debt.Terms.MinimumPayment);
        Assert.Null(debt.Terms.NextDueDate);
        Assert.Equal(100m, debt.Terms.Balance);

        var outcome = Assert.Single(PayoffRollover.Compare(input).Rollover.Debts);
        Assert.Equal(DebtScheduleStop.DueDateUnknown, outcome.Stop);
        Assert.Null(outcome.Minimum);
    }

    [Fact]
    public void Prepare_LeavesOutAMissingBalanceAndListsIt()
    {
        var prepared = HouseholdRecovery.Prepare(
            "USD",
            Today,
            [
                Debt(BlankId, "Blank", balance: null, apr: 12m, minimum: 25m, due: new DateOnly(2026, 1, 15)),
                Debt(CardId, "Card", balance: 100m, apr: 12m, minimum: 25m, due: new DateOnly(2026, 1, 15)),
                Debt(
                    OtherBlankId,
                    "Other blank",
                    balance: null,
                    apr: null,
                    minimum: null,
                    due: null,
                    currency: "CAD")
            ],
            0m);

        Assert.Equal(CardId, Assert.Single(prepared.Rollover.Debts).Terms.DebtId);
        Assert.Equal(
            [new HouseholdRecoveryMissingBalance(BlankId, "Blank"), new HouseholdRecoveryMissingBalance(OtherBlankId, "Other blank")],
            prepared.MissingBalance);
    }

    [Fact]
    public void Prepare_SharedExtraShortensThePayoffAndLowersThatDaysCash()
    {
        var debts = new[]
        {
            Debt(CardId, "Card", balance: 300m, apr: 0m, minimum: 50m, due: new DateOnly(2026, 1, 15))
        };
        var baseline = Project(debts, 0m);
        var extra = Project(debts, 50m);
        var due = new DateOnly(2026, 1, 15);

        Assert.Equal(0m, baseline.Extra);
        Assert.Equal(50m, extra.Extra);
        Assert.Equal(new DateOnly(2026, 6, 15), baseline.PaidOffOn);
        Assert.Equal(new DateOnly(2026, 3, 15), extra.PaidOffOn);
        Assert.Equal(50m, DebtPaid(baseline.Outlook, due));
        Assert.Equal(100m, DebtPaid(extra.Outlook, due));
        Assert.Equal(950m, CashOn(baseline.Outlook, due));
        Assert.Equal(900m, CashOn(extra.Outlook, due));
    }

    private static (decimal Extra, DateOnly? PaidOffOn, HouseholdCashOutlookReport Outlook) Project(
        IReadOnlyList<HouseholdRecoveryDebt> debts,
        decimal monthlyExtra)
    {
        var prepared = HouseholdRecovery.Prepare("USD", Today, debts, monthlyExtra);
        var comparison = PayoffRollover.Compare(prepared.Rollover);
        var outlook = HouseholdCashOutlook.Project(
            new HouseholdCashOutlookInput("USD", Today, 1000m, [], [], 0m, []),
            prepared.Rollover.Debts,
            comparison);
        return (comparison.MonthlyExtra, comparison.Rollover.PaidOffOn, outlook);
    }

    private static decimal DebtPaid(HouseholdCashOutlookReport outlook, DateOnly date)
    {
        return outlook.Rollover.Typical.Days
            .Where(day => day.Date == date)
            .SelectMany(day => day.Events)
            .Where(item => item.Kind == CashFlowKind.DebtPayment)
            .Sum(item => item.Amount);
    }

    private static decimal CashOn(HouseholdCashOutlookReport outlook, DateOnly date)
    {
        return outlook.Rollover.Typical.Days.Single(day => day.Date == date).Cash;
    }

    private static HouseholdRecoveryDebt Debt(
        Guid id,
        string name,
        decimal? balance,
        decimal? apr,
        decimal? minimum,
        DateOnly? due,
        string currency = "USD",
        decimal? limit = null)
    {
        return new HouseholdRecoveryDebt(
            id,
            name,
            currency,
            DebtKind.Revolving,
            balance,
            apr,
            null,
            null,
            minimum,
            null,
            due,
            limit);
    }
}
