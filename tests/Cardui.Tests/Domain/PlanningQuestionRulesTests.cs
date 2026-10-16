using Cardui.Api.Domain.Planning;
using Cardui.Api.Models;
using Xunit;

namespace Cardui.Tests.Domain;

public class PlanningQuestionRulesTests
{
    private static readonly Guid CardId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private static readonly Guid StoreId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private static readonly Guid LoanId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [Fact]
    public void Choose_StartsWithCashAndTheNextPaycheck()
    {
        var questions = PlanningQuestionRules.Choose(Facts());

        Assert.Equal(2, questions.Count);
        Assert.Equal(PlanningQuestionTopic.CashOnHand, questions[0].Topic);
        Assert.Equal("CashOnHand", questions[0].Key);
        Assert.Null(questions[0].DebtId);
        Assert.Equal("How much cash can you use today?", questions[0].Text);
        Assert.Equal(
            "The next 30 days start from that amount. If you do not know it, say so. An unknown balance stays unknown and is not treated as zero.",
            questions[0].Reason);
        Assert.Equal(PlanningQuestionTopic.NextPaycheck, questions[1].Topic);
        Assert.Equal(
            "When is the next paycheck, and about how much do you take home?",
            questions[1].Text);
        Assert.Equal(
            "A bill that comes due before that date has to be covered by cash you already have. A monthly average is not cash on that date. If you do not know yet, say so.",
            questions[1].Reason);
    }

    [Fact]
    public void Choose_LeavesALaterDetailUntilCashIsKnown()
    {
        var questions = PlanningQuestionRules.Choose(Facts(
            debts: [Debt(CardId, "Card", missingApr: true)]));

        Assert.Equal(
            [PlanningQuestionTopic.CashOnHand, PlanningQuestionTopic.NextPaycheck],
            questions.Select(question => question.Topic));
    }

    [Fact]
    public void Choose_SkipsADetailThePersonLeftUnknown()
    {
        var questions = PlanningQuestionRules.Choose(Facts(settled: ["  cashonhand  "]));

        Assert.Equal(
            [PlanningQuestionTopic.NextPaycheck, PlanningQuestionTopic.NextEssentialBill],
            questions.Select(question => question.Topic));
        Assert.Equal(
            "What essential bill has to be paid next, and about how much is it?",
            questions[1].Text);
        Assert.Equal(
            "An essential bill is cash that has to leave. The date shows whether the next paycheck arrives in time. If you do not know yet, say so. That does not make the bill zero.",
            questions[1].Reason);
    }

    [Fact]
    public void Choose_AsksWhetherStaleCashIsStillUsable()
    {
        var questions = PlanningQuestionRules.Choose(Facts(
            cashAccounts: 2,
            stale: 2,
            cash: 500.004m,
            currency: "usd",
            incomes: [Pay("Paycheck")],
            essentialBill: true,
            debts: [Paid(CardId, "Card")],
            living: 400m));

        var question = Assert.Single(questions);
        Assert.Equal(PlanningQuestionTopic.StaleCash, question.Topic);
        Assert.Equal(
            "Connected balances counted as cash are more than two days old, so is the cash you can use still about 500.00 USD?",
            question.Text);
        Assert.Equal(
            "An old balance can hide a shortfall. If you are not sure, say so. The plan keeps the last balance and says it may be old.",
            question.Reason);
    }

    [Fact]
    public void Choose_DoesNotAskAboutStaleCashWhenNoCashAccountIsRecorded()
    {
        var questions = PlanningQuestionRules.Choose(Facts(stale: 1));

        Assert.Equal(PlanningQuestionTopic.CashOnHand, questions[0].Topic);
        Assert.DoesNotContain(questions, question => question.Topic == PlanningQuestionTopic.StaleCash);
    }

    [Fact]
    public void Choose_AsksTheEssentialBillBeforeUnreliablePayWhenNothingIsDue()
    {
        var questions = PlanningQuestionRules.Choose(Facts(
            cashAccounts: 1,
            incomes: [Pay("Side job", IncomeReliability.Variable)]));

        Assert.Equal(
            [PlanningQuestionTopic.NextEssentialBill],
            questions.Select(question => question.Topic));
    }

    [Fact]
    public void Choose_AsksWhetherUnreliablePayShouldBeLeftOut()
    {
        var soon = Pay("Later", IncomeReliability.Uncertain, new DateOnly(2026, 11, 1));
        var first = Pay("Side job", IncomeReliability.Variable, new DateOnly(2026, 10, 20));
        var questions = PlanningQuestionRules.Choose(Facts(
            cashAccounts: 1,
            incomes: [soon, first],
            essentialBill: true));

        var question = Assert.Single(questions);
        Assert.Equal(PlanningQuestionTopic.UnreliablePay, question.Topic);
        Assert.Equal(
            "Is the next payment from Side job expected to arrive, or should the plan leave it out?",
            question.Text);
        Assert.Equal(
            "Variable and uncertain pay are not counted as cash that will arrive. Leaving it out does not turn it into a steady paycheck. If you are not sure, say so.",
            question.Reason);
    }

    [Fact]
    public void Choose_KeepsASteadyPaycheckFromTheUnreliableQuestion()
    {
        var questions = PlanningQuestionRules.Choose(Facts(
            cashAccounts: 1,
            incomes:
            [
                Pay("Paycheck"),
                Pay("Side job", IncomeReliability.Variable)
            ],
            essentialBill: true,
            debts: [Paid(CardId, "Card")],
            living: 100m));

        Assert.Empty(questions);
    }

    [Fact]
    public void Choose_AsksThePassedMinimumBeforeALaterDebt()
    {
        var questions = PlanningQuestionRules.Choose(Facts(
            cashAccounts: 1,
            incomes: [Pay("Paycheck")],
            essentialBill: true,
            debts:
            [
                Debt(StoreId, "Store", missingDue: false, due: new DateOnly(2026, 11, 1)),
                Debt(
                    CardId,
                    "Card",
                    missingDue: false,
                    due: new DateOnly(2026, 10, 1),
                    duePassed: true),
                Debt(LoanId, "Loan")
            ]));

        Assert.Equal(2, questions.Count);
        Assert.Equal(CardId, questions[0].DebtId);
        Assert.Equal("What is the minimum payment on Card?", questions[0].Text);
        Assert.Equal(
            "That payment is cash that has to leave. A blank amount stays unknown and is not stored as zero. The interest rate can wait. If you do not know, say so.",
            questions[0].Reason);
        Assert.Equal(StoreId, questions[1].DebtId);
        Assert.DoesNotContain(questions, question => question.DebtId == LoanId);
    }

    [Fact]
    public void Choose_AsksOnlyTheBlankPartOfADebtPayment()
    {
        var questions = PlanningQuestionRules.Choose(Facts(
            cashAccounts: 1,
            incomes: [Pay("Paycheck")],
            essentialBill: true,
            debts: [Debt(CardId, "Card", missingMinimum: false, due: null)]));

        var question = Assert.Single(questions);
        Assert.Equal("When is the next payment due on Card?", question.Text);
        Assert.Equal(
            "That date is when cash has to leave. The amount already saved stays as it is. If you do not know the date, say so.",
            question.Reason);
    }

    [Fact]
    public void Choose_SkipsADebtWhosePaymentIsRecorded()
    {
        var questions = PlanningQuestionRules.Choose(Facts(
            cashAccounts: 1,
            incomes: [Pay("Paycheck")],
            essentialBill: true,
            debts: [Paid(CardId, "Card")],
            living: 80m));

        Assert.Empty(questions);
    }

    [Fact]
    public void Choose_HoldsTheRateUntilThePaymentIsKnown()
    {
        var questions = PlanningQuestionRules.Choose(Facts(
            cashAccounts: 1,
            incomes: [Pay("Paycheck")],
            essentialBill: true,
            debts: [Debt(CardId, "Card", missingApr: true, missingMinimum: true)]));

        var question = Assert.Single(questions);
        Assert.Equal(PlanningQuestionTopic.DebtPayment, question.Topic);
    }

    [Fact]
    public void Choose_AsksOrdinarySpendingThenTheRateOnceCashIsSettled()
    {
        var questions = PlanningQuestionRules.Choose(Facts(
            cashAccounts: 1,
            incomes: [Pay("Paycheck")],
            essentialBill: true,
            debts:
            [
                Debt(StoreId, "Store", missingApr: true, missingMinimum: false, missingDue: false),
                Debt(CardId, "Card", missingApr: true, missingMinimum: false, missingDue: false),
                Debt(LoanId, "Loan", missingApr: true, missingMinimum: false, missingDue: false)
            ]));

        Assert.Equal(2, questions.Count);
        Assert.Equal(PlanningQuestionTopic.LivingSpending, questions[0].Topic);
        Assert.Equal(
            "About how much do you need each month for ordinary living, after bills you already listed?",
            questions[0].Text);
        Assert.Equal(
            "That spending leaves cash even when it is not one bill. Until it is set, the estimate of what you can spend leaves it out. If you do not know yet, say so. Unknown is not treated as zero.",
            questions[0].Reason);
        Assert.Equal(PlanningQuestionTopic.DebtApr, questions[1].Topic);
        Assert.Equal(CardId, questions[1].DebtId);
        Assert.Equal(
            "What is the interest rate on Card?",
            questions[1].Text);
        Assert.Equal(
            "The rate changes which debt costs more over time. It does not change the cash that has to leave this month. If you do not know it, say so. The rate stays unknown.",
            questions[1].Reason);
    }

    [Fact]
    public void Choose_AsksTheRateAfterThePaymentWasLeftUnknown()
    {
        var questions = PlanningQuestionRules.Choose(Facts(
            cashAccounts: 1,
            incomes: [Pay("Paycheck")],
            essentialBill: true,
            debts: [Debt(CardId, "Card", missingApr: true)],
            living: 50m,
            settled: [PlanningQuestionRules.Key(PlanningQuestionTopic.DebtPayment, CardId)]));

        var question = Assert.Single(questions);
        Assert.Equal(PlanningQuestionTopic.DebtApr, question.Topic);
        Assert.Equal(CardId, question.DebtId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("0")]
    [InlineData("DebtPayment")]
    [InlineData("CashOnHand:aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")]
    public void TryParseKey_RejectsAKeyThisRuleDoesNotDefine(string? key)
    {
        Assert.False(PlanningQuestionRules.TryParseKey(key, out _, out _));
    }

    [Fact]
    public void TryParseKey_ReadsADebtQuestion()
    {
        var key = PlanningQuestionRules.Key(PlanningQuestionTopic.DebtApr, CardId);

        var parsed = PlanningQuestionRules.TryParseKey(key.ToUpperInvariant(), out var topic, out var debtId);

        Assert.True(parsed);
        Assert.Equal(PlanningQuestionTopic.DebtApr, topic);
        Assert.Equal(CardId, debtId);
        Assert.Equal(key, PlanningQuestionRules.Key(topic, debtId));
    }

    #region Private Methods

    private static PlanningQuestionFacts Facts(
        int cashAccounts = 0,
        int stale = 0,
        decimal cash = 0,
        string currency = "USD",
        IReadOnlyList<PlanningIncomeFact>? incomes = null,
        bool essentialBill = false,
        IReadOnlyList<PlanningDebtQuestionFact>? debts = null,
        decimal? living = null,
        IReadOnlyList<string>? settled = null)
    {
        return new PlanningQuestionFacts(
            cashAccounts,
            stale,
            cash,
            currency,
            incomes ?? [],
            essentialBill,
            debts ?? [],
            living,
            settled ?? []);
    }

    private static PlanningIncomeFact Pay(
        string name,
        IncomeReliability reliability = IncomeReliability.Steady,
        DateOnly? next = null)
    {
        return new PlanningIncomeFact(
            Guid.NewGuid(),
            name,
            reliability,
            next ?? new DateOnly(2026, 10, 20));
    }

    private static PlanningDebtQuestionFact Debt(
        Guid id,
        string name,
        bool missingMinimum = true,
        bool missingDue = true,
        bool missingApr = false,
        DateOnly? due = null,
        bool duePassed = false)
    {
        return new PlanningDebtQuestionFact(
            id,
            name,
            missingMinimum,
            missingDue,
            missingApr,
            due,
            duePassed);
    }

    private static PlanningDebtQuestionFact Paid(Guid id, string name)
    {
        return Debt(
            id,
            name,
            missingMinimum: false,
            missingDue: false,
            due: new DateOnly(2026, 10, 20));
    }

    #endregion
}
