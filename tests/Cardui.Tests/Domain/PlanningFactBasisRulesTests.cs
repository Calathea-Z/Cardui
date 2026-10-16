using Cardui.Api.Domain.Debts;
using Cardui.Api.Domain.Planning;
using Cardui.Api.Models;
using Xunit;

namespace Cardui.Tests.Domain;

public class PlanningFactBasisRulesTests
{
    private static readonly Guid SubjectId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private static readonly Guid RelatedId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private static readonly DateOnly Today = new(2026, 10, 16);

    [Fact]
    public void Describe_LabelsManualCashAsThePersonsStatement()
    {
        var fact = Describe(Subject(
            PlanningFactKind.CashBalance,
            recordSource: FinancialRecordSource.Manual));

        Assert.Equal(PlanningFactBasis.UserStatement, fact.Basis);
        Assert.Equal(PlanningFactUnit.Money, fact.Unit);
        Assert.False(fact.ConfirmedObligation);
        Assert.Equal("The person entered this.", fact.Explanation);
        Assert.Equal("That fact is not a bill.", PlanningFactBasisRules.RejectAsObligation(fact.Basis, fact.Kind));
    }

    [Theory]
    [InlineData(FinancialRecordSource.Plaid)]
    [InlineData(FinancialRecordSource.Csv)]
    public void Describe_LabelsConnectedAndFileCashAsImported(FinancialRecordSource source)
    {
        var fact = Describe(Subject(PlanningFactKind.CashBalance, recordSource: source));

        Assert.Equal(PlanningFactBasis.ImportedFact, fact.Basis);
        Assert.False(fact.ConfirmedObligation);
        Assert.Equal("This amount came from a connected account or a file.", fact.Explanation);
        Assert.Equal(
            "An imported amount is not a bill until it is saved.",
            PlanningFactBasisRules.RejectAsObligation(fact.Basis, fact.Kind));
    }

    [Fact]
    public void Describe_KeepsAPaycheckAndStripsTheDateFromItsAverage()
    {
        var payment = Describe(Subject(
            PlanningFactKind.IncomePayment,
            amount: 2000m,
            asOf: Today));
        var average = Describe(Subject(
            PlanningFactKind.IncomeMonthlyAverage,
            amount: 4333.33m,
            asOf: Today));

        Assert.Equal(PlanningFactBasis.UserStatement, payment.Basis);
        Assert.Equal(Today, payment.AsOf);
        Assert.Equal(PlanningFactBasis.Estimate, average.Basis);
        Assert.Null(average.AsOf);
        Assert.Equal(4333.33m, average.Amount);
        Assert.False(average.ConfirmedObligation);
        Assert.Equal(
            "This is a yearly average spread across months. It is not cash on a date.",
            average.Explanation);
        Assert.Equal(
            "An estimate is not a confirmed bill.",
            PlanningFactBasisRules.RejectAsObligation(average.Basis, average.Kind));
    }

    [Fact]
    public void Describe_ConfirmsOnlyASavedBill()
    {
        var fact = Describe(Subject(PlanningFactKind.Bill, name: "Rent", amount: 900m, asOf: Today));

        Assert.Equal(PlanningFactBasis.UserStatement, fact.Basis);
        Assert.True(fact.ConfirmedObligation);
        Assert.True(PlanningFactBasisRules.IsConfirmedObligation(fact.Basis, fact.Kind));
        Assert.Null(PlanningFactBasisRules.RejectAsObligation(fact.Basis, fact.Kind));
        Assert.Equal("The person saved this bill.", fact.Explanation);
    }

    [Theory]
    [InlineData(FinancialRecordSource.Manual)]
    [InlineData(FinancialRecordSource.Plaid)]
    public void Describe_KeepsAnInferredExpenseUnconfirmed(FinancialRecordSource source)
    {
        var fact = Describe(Subject(
            PlanningFactKind.RecurringExpense,
            name: " Netflix ",
            detail: " netflix ",
            amount: 15.99m,
            asOf: Today,
            recordSource: source));

        Assert.Equal("Netflix", fact.Name);
        Assert.Equal("netflix", fact.Detail);
        Assert.Equal(PlanningFactBasis.UnverifiedSuggestion, fact.Basis);
        Assert.Equal(15.99m, fact.Amount);
        Assert.Equal(Today, fact.AsOf);
        Assert.False(fact.ConfirmedObligation);
        Assert.False(PlanningFactBasisRules.IsConfirmedObligation(fact.Basis, fact.Kind));
        Assert.Equal(
            "This is a payment pattern noticed in activity. The amount is one typical payment. It is not a bill until the person saves it.",
            fact.Explanation);
        Assert.Equal(
            "An inferred expense is not a bill until it is saved.",
            PlanningFactBasisRules.RejectAsObligation(fact.Basis, fact.Kind));
    }

    [Fact]
    public void Describe_DoesNotTurnASuggestedAccountIntoABalance()
    {
        var fact = Describe(Subject(
            PlanningFactKind.DebtMatch,
            name: "Visa 4821",
            detail: " Visa ",
            amount: 640m,
            asOf: Today,
            relatedId: RelatedId));

        Assert.Equal(PlanningFactBasis.UnverifiedSuggestion, fact.Basis);
        Assert.Equal(PlanningFactUnit.None, fact.Unit);
        Assert.Null(fact.Amount);
        Assert.Null(fact.AsOf);
        Assert.Equal(RelatedId, fact.RelatedId);
        Assert.Equal("Visa", fact.Detail);
        Assert.False(fact.ConfirmedObligation);
        Assert.Equal(
            "This account looks like the debt. It is not followed until the person chooses.",
            fact.Explanation);
        Assert.Equal(
            "A suggested account is not followed until the person chooses.",
            PlanningFactBasisRules.RejectAsObligation(fact.Basis, fact.Kind));
    }

    [Fact]
    public void Describe_LabelsASyncedDebtBalanceAsImported()
    {
        var fact = Describe(Subject(
            PlanningFactKind.DebtBalance,
            amount: 640m,
            asOf: Today,
            fieldSource: DebtFieldSource.Synced));

        Assert.Equal(PlanningFactBasis.ImportedFact, fact.Basis);
        Assert.Equal(640m, fact.Amount);
        Assert.False(fact.ConfirmedObligation);
    }

    [Theory]
    [InlineData(DebtFieldSource.Manual)]
    [InlineData(DebtFieldSource.Override)]
    public void Describe_LabelsThePersonsDebtBalanceAsAStatement(DebtFieldSource source)
    {
        var fact = Describe(Subject(
            PlanningFactKind.DebtBalance,
            fieldSource: source));

        Assert.Equal(PlanningFactBasis.UserStatement, fact.Basis);
        Assert.Equal("The person entered this.", fact.Explanation);
    }

    [Fact]
    public void Describe_KeepsAKnownZeroMinimumAndDropsAnAmountOnADueDate()
    {
        var minimum = Describe(Subject(PlanningFactKind.DebtMinimum, amount: 0m));
        var due = Describe(Subject(PlanningFactKind.DebtDueDate, amount: 25m, asOf: Today));
        var apr = Describe(Subject(PlanningFactKind.DebtApr, amount: 19.99m));

        Assert.Equal(0m, minimum.Amount);
        Assert.Equal(PlanningFactUnit.Money, minimum.Unit);
        Assert.False(minimum.ConfirmedObligation);
        Assert.Null(due.Amount);
        Assert.Equal(Today, due.AsOf);
        Assert.Equal(PlanningFactUnit.None, due.Unit);
        Assert.Equal(PlanningFactUnit.Percent, apr.Unit);
        Assert.Equal(19.99m, apr.Amount);
        Assert.Null(apr.Currency);
    }

    [Fact]
    public void Describe_KeepsANoteFromBecomingAnAmount()
    {
        var fact = Describe(Subject(
            PlanningFactKind.ConversationNote,
            name: " Question:CashOnHand ",
            detail: " unknown ",
            amount: 500m,
            asOf: Today));

        Assert.Equal(PlanningFactBasis.UserStatement, fact.Basis);
        Assert.Equal("Question:CashOnHand", fact.Name);
        Assert.Equal("unknown", fact.Detail);
        Assert.Null(fact.Amount);
        Assert.Null(fact.AsOf);
        Assert.Equal(PlanningFactUnit.None, fact.Unit);
        Assert.False(fact.ConfirmedObligation);
        Assert.Equal(
            "The person wrote this. It is not an amount, and unknown does not mean zero.",
            fact.Explanation);
    }

    [Theory]
    [InlineData(PlanningFactKind.CashBalance)]
    [InlineData(PlanningFactKind.DebtBalance)]
    [InlineData(PlanningFactKind.DebtCreditLimit)]
    public void Describe_RejectsAMissingSource(PlanningFactKind kind)
    {
        var described = PlanningFactBasisRules.TryDescribe(Subject(kind), out var fact, out var error);

        Assert.False(described);
        Assert.Null(fact);
        Assert.Equal("Choose where this amount came from.", error);
    }

    [Fact]
    public void Describe_RejectsAnUnknownSource()
    {
        var described = PlanningFactBasisRules.TryDescribe(
            Subject(PlanningFactKind.CashBalance, recordSource: (FinancialRecordSource)99),
            out _,
            out var error);

        Assert.False(described);
        Assert.Equal("Choose where this amount came from.", error);
    }

    [Fact]
    public void Describe_RejectsABlankNameAndAnUnknownKind()
    {
        var blank = PlanningFactBasisRules.TryDescribe(
            Subject(PlanningFactKind.Bill, name: "  "),
            out _,
            out var blankError);
        var unknown = PlanningFactBasisRules.TryDescribe(
            Subject((PlanningFactKind)99),
            out _,
            out var unknownError);

        Assert.False(blank);
        Assert.Equal("Name this fact.", blankError);
        Assert.False(unknown);
        Assert.Equal("Choose a fact.", unknownError);
        Assert.Equal(
            "Choose a fact.",
            PlanningFactBasisRules.RejectAsObligation((PlanningFactBasis)99, PlanningFactKind.Bill));
    }

    #region Private Methods

    private static PlanningFact Describe(PlanningFactSubject subject)
    {
        Assert.True(PlanningFactBasisRules.TryDescribe(subject, out var fact, out var error), error);
        return fact!;
    }

    private static PlanningFactSubject Subject(
        PlanningFactKind kind,
        string name = "Checking",
        string? detail = null,
        decimal? amount = 10m,
        DateOnly? asOf = null,
        Guid? relatedId = null,
        FinancialRecordSource? recordSource = null,
        DebtFieldSource? fieldSource = null)
    {
        return new PlanningFactSubject(
            kind,
            SubjectId,
            relatedId,
            name,
            detail,
            amount,
            kind == PlanningFactKind.DebtApr ? null : "USD",
            asOf,
            recordSource,
            fieldSource);
    }

    #endregion
}
