namespace Cardui.Api.Domain.Planning;

/// <summary>
/// Which detail the next question is about.
/// The member name is the note name. A question about one debt appends that debt's id.
/// Cash on hand, a stale balance, the next paycheck, an essential bill, and a debt payment come first.
/// The interest rate and ordinary monthly spending wait until those cash questions are answered or left unknown.
/// </summary>
public enum PlanningQuestionTopic
{
    CashOnHand,
    StaleCash,
    NextPaycheck,
    UnreliablePay,
    NextEssentialBill,
    DebtPayment,
    DebtApr,
    LivingSpending
}
