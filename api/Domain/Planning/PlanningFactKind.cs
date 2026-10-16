namespace Cardui.Api.Domain.Planning;

/// <summary>
/// What a labeled planning fact is about.
/// A bill is one the person saved. A recurring expense is only a pattern noticed in activity.
/// A debt match is a connected account that looks like a debt, and it is not followed yet.
/// The monthly average is a yearly figure with no date. A conversation note is words, not an amount.
/// The member name is the JSON text. This value is not stored.
/// </summary>
public enum PlanningFactKind
{
    CashBalance,
    IncomePayment,
    IncomeMonthlyAverage,
    Bill,
    RecurringExpense,
    DebtBalance,
    DebtCreditLimit,
    DebtMinimum,
    DebtDueDate,
    DebtApr,
    DebtMatch,
    LivingSpending,
    ConversationNote
}
