using Cardui.Api.Domain;
using Cardui.Api.Models;
using Xunit;

namespace Cardui.Tests.Domain;

public class ObligationRulesTests
{
    [Fact]
    public void TryNormalize_TrimsTheNameAndKeepsOnePayment()
    {
        var accountId = Guid.NewGuid();
        var ok = ObligationRules.TryNormalize(
            "  Rent  ",
            1450.5m,
            ObligationCadence.Monthly,
            new DateOnly(2026, 10, 1),
            accountId,
            ObligationFlexibility.Essential,
            out var draft,
            out var error);

        Assert.True(ok);
        Assert.Equal("", error);
        Assert.Equal("Rent", draft.Name);
        Assert.Equal(1450.5m, draft.Amount);
        Assert.Equal(ObligationCadence.Monthly, draft.Cadence);
        Assert.Equal(new DateOnly(2026, 10, 1), draft.NextDueDate);
        Assert.Equal(accountId, draft.AccountId);
        Assert.Equal(ObligationFlexibility.Essential, draft.Flexibility);
    }

    [Fact]
    public void TryNormalize_ClearsAnEmptyAccount()
    {
        var ok = ObligationRules.TryNormalize(
            "Phone",
            80m,
            ObligationCadence.Monthly,
            new DateOnly(2026, 10, 12),
            Guid.Empty,
            ObligationFlexibility.Flexible,
            out var draft,
            out var error);

        Assert.True(ok);
        Assert.Equal("", error);
        Assert.Null(draft.AccountId);
        Assert.Equal(ObligationFlexibility.Flexible, draft.Flexibility);
    }

    [Theory]
    [InlineData(0, "Enter the amount for one payment.")]
    [InlineData(-20, "Enter the amount for one payment.")]
    [InlineData(10.125, "Enter the amount in dollars and cents.")]
    public void TryNormalize_RejectsAnAmountThatIsNotOnePayment(double amount, string message)
    {
        var ok = ObligationRules.TryNormalize(
            "Rent",
            (decimal)amount,
            ObligationCadence.Monthly,
            new DateOnly(2026, 10, 1),
            null,
            ObligationFlexibility.Essential,
            out _,
            out var error);

        Assert.False(ok);
        Assert.Equal(message, error);
    }

    [Fact]
    public void TryNormalize_RejectsAMissingChoiceOrADateOutsideTheRange()
    {
        Assert.Equal(
            "A bill name is required.",
            Reject(name: "  ").error);
        Assert.Equal(
            "Choose how often this bill is due.",
            Reject(cadence: null).error);
        Assert.Equal(
            "Choose whether this bill is essential or flexible.",
            Reject(flexibility: null).error);
        Assert.Equal(
            "Enter the next due date.",
            Reject(nextDueDate: new DateOnly(1999, 12, 31)).error);
        Assert.Equal(
            "That amount is too large.",
            Reject(amount: ObligationRules.MaxAmount + 0.01m).error);
    }

    private static (bool ok, string error) Reject(
        string name = "Rent",
        decimal amount = 100m,
        ObligationCadence? cadence = ObligationCadence.Monthly,
        DateOnly? nextDueDate = null,
        ObligationFlexibility? flexibility = ObligationFlexibility.Essential)
    {
        var ok = ObligationRules.TryNormalize(
            name,
            amount,
            cadence,
            nextDueDate ?? new DateOnly(2026, 10, 1),
            null,
            flexibility,
            out _,
            out var error);
        return (ok, error);
    }
}
