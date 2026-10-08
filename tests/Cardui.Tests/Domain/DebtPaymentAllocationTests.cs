using Cardui.Api.Domain.Recovery;
using Cardui.Api.Models;
using Xunit;

namespace Cardui.Tests.Domain;

public class DebtPaymentAllocationTests
{
    private static readonly Guid FirstId = Guid.Parse("30000000-0000-0000-0000-000000000001");
    private static readonly Guid SecondId = Guid.Parse("30000000-0000-0000-0000-000000000002");
    private static readonly Guid ThirdId = Guid.Parse("30000000-0000-0000-0000-000000000003");
    private static readonly DateOnly Due = new(2026, 4, 1);

    [Fact]
    public void Apply_GivesExtraToTheFirstDebtInTheSuppliedOrder()
    {
        var result = DebtPaymentAllocation.Apply(
            [
                Debt(FirstId, "Low", 200m, 0m, 25m),
                Debt(SecondId, "High", 80m, 10m, 20m)
            ],
            30m);

        var first = result.Lines[0].Period!;
        var second = result.Lines[1].Period!;
        Assert.Equal(30m, first.ExtraPaid);
        Assert.Equal(55m, first.Payment);
        Assert.Equal(145m, first.EndingBalance);
        Assert.Equal(0m, second.ExtraPaid);
        Assert.Equal(20m, second.MinimumPaid);
        Assert.Equal(0.67m, second.Interest);
        Assert.Equal(60.67m, second.EndingBalance);
        Assert.Equal(30m, result.ExtraApplied);
        Assert.Equal(0m, result.ExtraLeft);
    }

    [Fact]
    public void Apply_ContinuesExtraAfterADebtIsPaidOffAndReturnsTheRest()
    {
        var result = DebtPaymentAllocation.Apply(
            [
                Debt(FirstId, "First", 200m, 0m, 25m),
                Debt(SecondId, "Second", 80m, 10m, 20m)
            ],
            250m);

        Assert.Equal(0m, result.Lines[0].Period!.EndingBalance);
        Assert.Equal(175m, result.Lines[0].Period!.ExtraPaid);
        Assert.Equal(0m, result.Lines[1].Period!.EndingBalance);
        Assert.Equal(60.67m, result.Lines[1].Period!.ExtraPaid);
        Assert.Equal(235.67m, result.ExtraApplied);
        Assert.Equal(14.33m, result.ExtraLeft);
    }

    [Fact]
    public void Apply_SkipsADebtThatCannotBeCalculated()
    {
        var result = DebtPaymentAllocation.Apply(
            [
                Debt(FirstId, "First", 50m, 0m, 50m),
                Debt(SecondId, "Unknown", 80m, 10m, null),
                Debt(ThirdId, "Third", 40m, 0m, 10m)
            ],
            100m);

        Assert.Equal(0m, result.Lines[0].Period!.ExtraPaid);
        Assert.Equal(DebtScheduleStop.MinimumUnknown, result.Lines[1].Skip);
        Assert.Null(result.Lines[1].Period);
        Assert.Equal(30m, result.Lines[2].Period!.ExtraPaid);
        Assert.Equal(0m, result.Lines[2].Period!.EndingBalance);
        Assert.Equal(70m, result.ExtraLeft);
    }

    [Fact]
    public void Apply_PaysMinimumsWhenExtraIsZero()
    {
        var debt = new DebtAmortizationInput(
            FirstId,
            "Card",
            "USD",
            DebtKind.Revolving,
            100m,
            0m,
            null,
            null,
            25m,
            null,
            Due,
            40m);
        var result = DebtPaymentAllocation.Apply([debt], 0m);

        Assert.Equal(25m, result.Lines[0].Period!.Payment);
        Assert.Equal(0m, result.Lines[0].Period!.ExtraPaid);
        Assert.Equal(0m, result.ExtraApplied);
        Assert.Equal(75m, result.Lines[0].Period!.EndingBalance);
    }

    private static DebtAmortizationInput Debt(
        Guid id,
        string name,
        decimal balance,
        decimal? apr,
        decimal? minimum)
    {
        return new DebtAmortizationInput(
            id,
            name,
            "USD",
            DebtKind.Revolving,
            balance,
            apr,
            null,
            null,
            minimum,
            null,
            Due,
            0m);
    }
}
