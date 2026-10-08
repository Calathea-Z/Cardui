using Cardui.Api.Domain.Recovery;
using Cardui.Api.Models;
using Xunit;

namespace Cardui.Tests.Domain;

public class CashFlowScheduleTests
{
    private static readonly Guid PayId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid RentId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    private static readonly Guid GiftId = Guid.Parse("10000000-0000-0000-0000-000000000003");

    [Fact]
    public void Project_KeepsThreeBiweeklyPaychecksOnTheirDates()
    {
        var events = CashFlowSchedule.Project(
            [Pay("Pay", 1000m, IncomeCadence.Biweekly, new DateOnly(2026, 1, 2))],
            [],
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31));

        Assert.Equal(
            [
                new DateOnly(2026, 1, 2),
                new DateOnly(2026, 1, 16),
                new DateOnly(2026, 1, 30)
            ],
            events.Select(item => item.Date));
        Assert.All(events, item => Assert.Equal(1000m, item.Amount));
        Assert.All(events, item => Assert.Equal(CashFlowKind.Income, item.Kind));
        Assert.DoesNotContain(events, item => item.Amount == 2166.67m);
    }

    [Fact]
    public void Project_AppliesARaiseOnAndAfterItsDate()
    {
        var income = Pay(
            "Pay",
            1000m,
            IncomeCadence.Biweekly,
            new DateOnly(2026, 1, 2),
            [new DatedIncomeRaise(new DateOnly(2026, 2, 1), 1100m)]);

        var events = CashFlowSchedule.Project(
            [income],
            [],
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 2, 13));

        Assert.Equal(1000m, events.Single(item => item.Date == new DateOnly(2026, 1, 30)).Amount);
        Assert.Equal(1100m, events.Single(item => item.Date == new DateOnly(2026, 2, 13)).Amount);
    }

    [Fact]
    public void Project_RestoresAMonthEndBillAfterFebruary()
    {
        var events = CashFlowSchedule.Project(
            [],
            [Bill("Rent", 50m, ObligationCadence.Monthly, new DateOnly(2026, 1, 31))],
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 3, 31));

        Assert.Equal(
            [
                new DateOnly(2026, 1, 31),
                new DateOnly(2026, 2, 28),
                new DateOnly(2026, 3, 31)
            ],
            events.Select(item => item.Date));
    }

    [Fact]
    public void Project_IncludesAnIrregularPaymentOnce()
    {
        var events = CashFlowSchedule.Project(
            [Pay("Gift", 40m, IncomeCadence.Irregular, new DateOnly(2026, 1, 15), id: GiftId)],
            [],
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 6, 30));

        var gift = Assert.Single(events);
        Assert.Equal(new DateOnly(2026, 1, 15), gift.Date);
        Assert.Equal(40m, gift.Amount);
    }

    [Fact]
    public void Project_ListsSameDayIncomeBeforeTheBill()
    {
        var events = CashFlowSchedule.Project(
            [Pay("Pay", 1000m, IncomeCadence.Monthly, new DateOnly(2026, 1, 15))],
            [Bill("Rent", 800m, ObligationCadence.Monthly, new DateOnly(2026, 1, 15))],
            new DateOnly(2026, 1, 15),
            new DateOnly(2026, 1, 15));

        Assert.Equal(CashFlowKind.Income, events[0].Kind);
        Assert.Equal(CashFlowKind.Bill, events[1].Kind);
        Assert.Equal("USD", events[0].Currency);
        Assert.Equal("CAD", events[1].Currency);
    }

    [Fact]
    public void Project_IsEmptyWhenTheWindowEndsBeforeItStarts()
    {
        var events = CashFlowSchedule.Project(
            [Pay("Pay", 1000m, IncomeCadence.Monthly, new DateOnly(2026, 1, 15))],
            [],
            new DateOnly(2026, 2, 1),
            new DateOnly(2026, 1, 1));

        Assert.Empty(events);
    }

    [Fact]
    public void Combine_OrdersDebtAndSavingsAfterIncomeAndBills()
    {
        var debt = DebtAmortization.Project(new DebtAmortizationInput(
            Guid.Parse("10000000-0000-0000-0000-000000000004"),
            "Card",
            "USD",
            DebtKind.Revolving,
            100m,
            0m,
            null,
            null,
            100m,
            null,
            new DateOnly(2026, 1, 15),
            0m));
        var events = CashFlowSchedule.Combine(
        [
            ..CashFlowSchedule.Project(
                [Pay("Pay", 1000m, IncomeCadence.Monthly, new DateOnly(2026, 1, 15))],
                [Bill("Rent", 800m, ObligationCadence.Monthly, new DateOnly(2026, 1, 15))],
                new DateOnly(2026, 1, 15),
                new DateOnly(2026, 1, 15)),
            ..CashFlowSchedule.DebtPayments(debt),
            ..CashFlowSchedule.Savings(
                Guid.Parse("10000000-0000-0000-0000-000000000005"),
                "Reserve",
                "USD",
                [new SavingsContribution(new DateOnly(2026, 1, 15), 25m)])
        ]);

        Assert.Equal(
            [CashFlowKind.Income, CashFlowKind.Bill, CashFlowKind.DebtPayment, CashFlowKind.Savings],
            events.Select(item => item.Kind));
        Assert.Equal(100m, events.Single(item => item.Kind == CashFlowKind.DebtPayment).Amount);
    }

    private static DatedIncome Pay(
        string name,
        decimal amount,
        IncomeCadence cadence,
        DateOnly next,
        IReadOnlyList<DatedIncomeRaise>? raises = null,
        Guid? id = null)
    {
        return new DatedIncome(
            id ?? PayId,
            name,
            "USD",
            amount,
            cadence,
            next,
            raises ?? []);
    }

    private static DatedBill Bill(string name, decimal amount, ObligationCadence cadence, DateOnly next)
    {
        return new DatedBill(RentId, name, "CAD", amount, cadence, next);
    }
}
