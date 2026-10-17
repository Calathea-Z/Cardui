namespace Cardui.Api.Domain.Planning;

/// <summary>
/// The household facts used to choose the next questions.
/// CashAccountCount is cash in the planning currency. StaleCashAccountCount is connected cash older than two days.
/// LivingSpendingMonthly above zero is set. Null or zero means ordinary monthly spending is not set.
/// SettledKeys are details the person left unknown. A key does not fill in the missing fact.
/// </summary>
public sealed record PlanningQuestionFacts(
    int CashAccountCount,
    int StaleCashAccountCount,
    decimal CashTotal,
    string PlanningCurrency,
    IReadOnlyList<PlanningIncomeFact> Incomes,
    bool HasEssentialBill,
    IReadOnlyList<PlanningDebtQuestionFact> Debts,
    decimal? LivingSpendingMonthly,
    IReadOnlyList<string> SettledKeys);
