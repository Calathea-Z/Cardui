using Cardui.Api.Domain.Planning;
using Xunit;

namespace Cardui.Tests.Domain;

public class PlanningToolRulesTests
{
    [Fact]
    public void RejectSummary_RequiresAKnownSection()
    {
        Assert.Null(PlanningToolRules.RejectSummary(PlanningSummaryKind.Bills));
        Assert.Equal("Choose a summary.", PlanningToolRules.RejectSummary((PlanningSummaryKind)99));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RejectSuggestion_AllowsAHandEnteredBill(string? key)
    {
        Assert.Null(PlanningToolRules.RejectSuggestion(key));
    }

    [Fact]
    public void RejectSuggestion_KeepsANoticedPaymentOffTheBill()
    {
        Assert.Equal(
            "A suggestion is not a bill until it is saved.",
            PlanningToolRules.RejectSuggestion("  phone  "));
    }
}
