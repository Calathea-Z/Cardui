using System.Globalization;
using Cardui.Api.Domain.Accounts;
using Cardui.Api.Models;

namespace Cardui.Api.Domain.Planning;

/// <summary>
/// Chooses the next one or two questions from what the household has already recorded.
/// Dated cash comes first. A detail the person left unknown is skipped, and it is not filled in as zero.
/// </summary>
public static class PlanningQuestionRules
{
    /// <summary>
    /// The note value that means the person does not know this detail.
    /// </summary>
    public const string Unknown = "unknown";

    /// <summary>
    /// Returns at most two questions.
    /// When a dated cash gap is still open, both come from that list. The interest rate and ordinary monthly spending wait.
    /// A saved fact is not asked again. A settled key is not asked again, and it does not invent the fact.
    /// </summary>
    public static IReadOnlyList<PlanningQuestion> Choose(PlanningQuestionFacts facts)
    {
        var settled = Settle(facts.SettledKeys);
        var urgent = Urgent(facts, settled);
        var chosen = urgent.Count > 0 ? urgent : Later(facts, settled);
        return chosen.Take(2).ToList();
    }

    /// <summary>
    /// The note name for one question. A debt question appends that debt's id.
    /// </summary>
    public static string Key(PlanningQuestionTopic topic, Guid? debtId = null)
    {
        return debtId is Guid id
            ? topic + ":" + id.ToString("D")
            : topic.ToString();
    }

    /// <summary>
    /// Reads a note name back into a topic and, for a debt question, that debt's id.
    /// A number, a blank, or a debt id on a household question is rejected.
    /// </summary>
    public static bool TryParseKey(string? key, out PlanningQuestionTopic topic, out Guid? debtId)
    {
        topic = default;
        debtId = null;
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        var text = key.Trim();
        var separator = text.IndexOf(':');
        var name = separator < 0 ? text : text[..separator];
        var rest = separator < 0 ? null : text[(separator + 1)..];
        if (!TryTopic(name, out topic))
        {
            return false;
        }

        var needsDebt = topic is PlanningQuestionTopic.DebtPayment or PlanningQuestionTopic.DebtApr;
        if (!needsDebt)
        {
            return rest is null;
        }

        if (rest is null || !Guid.TryParse(rest, out var id) || id == Guid.Empty)
        {
            return false;
        }

        if (!string.Equals(Key(topic, id), text, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        debtId = id;
        return true;
    }

    #region Private Methods

    /// <summary>
    /// The note names the person already left unknown, ignoring case and end spaces.
    /// </summary>
    private static HashSet<string> Settle(IReadOnlyList<string> keys)
    {
        var settled = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in keys)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                settled.Add(key.Trim());
            }
        }

        return settled;
    }

    /// <summary>
    /// True when this detail was left unknown.
    /// </summary>
    private static bool IsSettled(
        HashSet<string> settled,
        PlanningQuestionTopic topic,
        Guid? debtId = null)
    {
        return settled.Contains(Key(topic, debtId));
    }

    /// <summary>
    /// True when the topic name is one this rule defines. A numeric value is rejected.
    /// </summary>
    private static bool TryTopic(string name, out PlanningQuestionTopic topic)
    {
        if (!Enum.TryParse(name, ignoreCase: true, out topic) || !Enum.IsDefined(topic))
        {
            return false;
        }

        return string.Equals(topic.ToString(), name, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Questions that decide whether near-term cash covers what has to leave.
    /// </summary>
    private static List<PlanningQuestion> Urgent(
        PlanningQuestionFacts facts,
        HashSet<string> settled)
    {
        var questions = new List<PlanningQuestion>();
        AddCash(facts, settled, questions);
        AddStaleCash(facts, settled, questions);
        AddPaycheck(facts, settled, questions);
        AddEssentialBill(facts, settled, questions);
        AddUnreliablePay(facts, settled, questions);
        AddDebtPayments(facts, settled, questions);
        return questions;
    }

    /// <summary>
    /// Questions that wait until the dated cash gaps are answered or left unknown.
    /// Ordinary monthly spending comes before an interest rate.
    /// </summary>
    private static List<PlanningQuestion> Later(
        PlanningQuestionFacts facts,
        HashSet<string> settled)
    {
        var questions = new List<PlanningQuestion>();
        AddLivingSpending(facts, settled, questions);
        AddDebtRates(facts, settled, questions);
        return questions;
    }

    /// <summary>
    /// Asks for cash that can be used today when no cash account is recorded.
    /// </summary>
    private static void AddCash(
        PlanningQuestionFacts facts,
        HashSet<string> settled,
        List<PlanningQuestion> questions)
    {
        if (facts.CashAccountCount > 0 || IsSettled(settled, PlanningQuestionTopic.CashOnHand))
        {
            return;
        }

        Add(
            questions,
            PlanningQuestionTopic.CashOnHand,
            null,
            "How much cash can you use today?",
            "The next 30 days start from that amount. If you do not know it, say so. An unknown balance stays unknown and is not treated as zero.");
    }

    /// <summary>
    /// Asks whether a connected cash balance older than two days is still the amount to use.
    /// </summary>
    private static void AddStaleCash(
        PlanningQuestionFacts facts,
        HashSet<string> settled,
        List<PlanningQuestion> questions)
    {
        if (facts.CashAccountCount == 0
            || facts.StaleCashAccountCount <= 0
            || IsSettled(settled, PlanningQuestionTopic.StaleCash))
        {
            return;
        }

        var balances = facts.StaleCashAccountCount == 1
            ? "A connected balance counted as cash is more than two days old"
            : "Connected balances counted as cash are more than two days old";
        Add(
            questions,
            PlanningQuestionTopic.StaleCash,
            null,
            balances + ", so is the cash you can use still about " + Money(facts.CashTotal, facts.PlanningCurrency) + "?",
            "An old balance can hide a shortfall. If you are not sure, say so. The plan keeps the last balance and says it may be old.");
    }

    /// <summary>
    /// Asks for the next paycheck when none is recorded.
    /// A monthly average is not a substitute for the date and the amount.
    /// </summary>
    private static void AddPaycheck(
        PlanningQuestionFacts facts,
        HashSet<string> settled,
        List<PlanningQuestion> questions)
    {
        if (facts.Incomes.Count > 0 || IsSettled(settled, PlanningQuestionTopic.NextPaycheck))
        {
            return;
        }

        Add(
            questions,
            PlanningQuestionTopic.NextPaycheck,
            null,
            "When is the next paycheck, and about how much do you take home?",
            "A bill that comes due before that date has to be covered by cash you already have. A monthly average is not cash on that date. If you do not know yet, say so.");
    }

    /// <summary>
    /// Asks for the next essential bill when none is recorded.
    /// A flexible bill does not answer it.
    /// </summary>
    private static void AddEssentialBill(
        PlanningQuestionFacts facts,
        HashSet<string> settled,
        List<PlanningQuestion> questions)
    {
        if (facts.HasEssentialBill || IsSettled(settled, PlanningQuestionTopic.NextEssentialBill))
        {
            return;
        }

        Add(
            questions,
            PlanningQuestionTopic.NextEssentialBill,
            null,
            "What essential bill has to be paid next, and about how much is it?",
            "An essential bill is cash that has to leave. The date shows whether the next paycheck arrives in time. If you do not know yet, say so. That does not make the bill zero.");
    }

    /// <summary>
    /// Asks whether pay that is not steady should be left out, once some cash is known to be due.
    /// One steady paycheck is enough to skip this. The question names the soonest unreliable payment.
    /// </summary>
    private static void AddUnreliablePay(
        PlanningQuestionFacts facts,
        HashSet<string> settled,
        List<PlanningQuestion> questions)
    {
        if (!AskUnreliable(facts) || IsSettled(settled, PlanningQuestionTopic.UnreliablePay))
        {
            return;
        }

        var income = SoonestUnreliable(facts.Incomes);
        Add(
            questions,
            PlanningQuestionTopic.UnreliablePay,
            null,
            "Is the next payment from " + income.Name + " expected to arrive, or should the plan leave it out?",
            "Variable and uncertain pay are not counted as cash that will arrive. Leaving it out does not turn it into a steady paycheck. If you are not sure, say so.");
    }

    /// <summary>
    /// True when every paycheck may not arrive, and a bill or a debt payment is already known.
    /// Until something is known to be due, the bill or the debt payment is the more useful question.
    /// </summary>
    private static bool AskUnreliable(PlanningQuestionFacts facts)
    {
        if (facts.Incomes.Count == 0
            || facts.Incomes.Any(income => income.Reliability == IncomeReliability.Steady))
        {
            return false;
        }

        return facts.HasEssentialBill
            || facts.Debts.Any(debt => !debt.MissingMinimum || !debt.MissingDueDate);
    }

    /// <summary>
    /// The unreliable paycheck with the soonest date. Names break a tie, then ids.
    /// </summary>
    private static PlanningIncomeFact SoonestUnreliable(IReadOnlyList<PlanningIncomeFact> incomes)
    {
        return incomes
            .OrderBy(income => income.NextPaymentDate)
            .ThenBy(income => income.Name, StringComparer.Ordinal)
            .ThenBy(income => income.Id)
            .First();
    }

    /// <summary>
    /// Asks for a missing minimum or due date. A passed due date with a missing minimum comes first.
    /// </summary>
    private static void AddDebtPayments(
        PlanningQuestionFacts facts,
        HashSet<string> settled,
        List<PlanningQuestion> questions)
    {
        var debts = facts.Debts
            .Where(debt => debt.MissingMinimum || debt.MissingDueDate)
            .Where(debt => !IsSettled(settled, PlanningQuestionTopic.DebtPayment, debt.DebtId))
            .OrderByDescending(debt => debt.DueDatePassed && debt.MissingMinimum)
            .ThenBy(debt => debt.NextDueDate ?? DateOnly.MaxValue)
            .ThenBy(debt => debt.Name, StringComparer.Ordinal)
            .ThenBy(debt => debt.DebtId);
        foreach (var debt in debts)
        {
            Add(questions, PlanningQuestionTopic.DebtPayment, debt.DebtId, PaymentText(debt), PaymentReason(debt));
        }
    }

    /// <summary>
    /// The payment question. It names only the part that is still blank.
    /// </summary>
    private static string PaymentText(PlanningDebtQuestionFact debt)
    {
        if (debt.MissingMinimum && debt.MissingDueDate)
        {
            return "What is the minimum payment on " + debt.Name + ", and when is it due?";
        }

        return debt.MissingMinimum
            ? "What is the minimum payment on " + debt.Name + "?"
            : "When is the next payment due on " + debt.Name + "?";
    }

    /// <summary>
    /// Why the payment detail matters. A blank amount stays unknown. The rate can wait.
    /// </summary>
    private static string PaymentReason(PlanningDebtQuestionFact debt)
    {
        return debt.MissingMinimum
            ? "That payment is cash that has to leave. A blank amount stays unknown and is not stored as zero. The interest rate can wait. If you do not know, say so."
            : "That date is when cash has to leave. The amount already saved stays as it is. If you do not know the date, say so.";
    }

    /// <summary>
    /// Asks for ordinary monthly spending once the dated cash questions are done.
    /// </summary>
    private static void AddLivingSpending(
        PlanningQuestionFacts facts,
        HashSet<string> settled,
        List<PlanningQuestion> questions)
    {
        if ((facts.LivingSpendingMonthly is decimal amount && amount > 0)
            || IsSettled(settled, PlanningQuestionTopic.LivingSpending))
        {
            return;
        }

        Add(
            questions,
            PlanningQuestionTopic.LivingSpending,
            null,
            "About how much do you need each month for ordinary living, after bills you already listed?",
            "That spending leaves cash even when it is not one bill. Until it is set, the estimate of what you can spend leaves it out. If you do not know yet, say so. Unknown is not treated as zero.");
    }

    /// <summary>
    /// Asks for a missing interest rate after that debt's payment is known or left unknown.
    /// </summary>
    private static void AddDebtRates(
        PlanningQuestionFacts facts,
        HashSet<string> settled,
        List<PlanningQuestion> questions)
    {
        var debts = facts.Debts
            .Where(debt => debt.MissingApr && PaymentKnown(debt, settled))
            .Where(debt => !IsSettled(settled, PlanningQuestionTopic.DebtApr, debt.DebtId))
            .OrderBy(debt => debt.Name, StringComparer.Ordinal)
            .ThenBy(debt => debt.DebtId);
        foreach (var debt in debts)
        {
            Add(
                questions,
                PlanningQuestionTopic.DebtApr,
                debt.DebtId,
                "What is the interest rate on " + debt.Name + "?",
                "The rate changes which debt costs more over time. It does not change the cash that has to leave this month. If you do not know it, say so. The rate stays unknown.");
        }
    }

    /// <summary>
    /// True when the minimum and the due date are both recorded, or the person left that payment unknown.
    /// </summary>
    private static bool PaymentKnown(PlanningDebtQuestionFact debt, HashSet<string> settled)
    {
        var fieldsKnown = !debt.MissingMinimum && !debt.MissingDueDate;
        return fieldsKnown || IsSettled(settled, PlanningQuestionTopic.DebtPayment, debt.DebtId);
    }

    /// <summary>
    /// Appends one question. The caller has already decided it is still open.
    /// </summary>
    private static void Add(
        List<PlanningQuestion> questions,
        PlanningQuestionTopic topic,
        Guid? debtId,
        string text,
        string reason)
    {
        questions.Add(new PlanningQuestion(topic, Key(topic, debtId), debtId, text, reason));
    }

    /// <summary>
    /// Cents and the planning currency, for a question about a balance.
    /// </summary>
    private static string Money(decimal amount, string? currency)
    {
        var text = AccountLedger.Round(amount).ToString("0.00", CultureInfo.InvariantCulture);
        if (string.IsNullOrWhiteSpace(currency))
        {
            return text;
        }

        return text + " " + currency.Trim().ToUpperInvariant();
    }

    #endregion
}
