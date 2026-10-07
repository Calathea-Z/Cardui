using Cardui.Api.Models;

namespace Cardui.Api.Domain.Debts;

/// <summary>
/// One recorded debt, plus the linked account balance when there is one.
/// A null balance, rate, minimum, due date, limit, term, or promotion is unknown.
/// LinkedBalance is null when the debt is not linked to an account.
/// Balance is the amount in use. On a followed debt that is the resolved balance, not a second copy.
/// Following is true when the debt follows its account. Freshness is null when it does not.
/// </summary>
public sealed record DebtSummaryInput(
    Guid DebtId,
    string Currency,
    DebtKind Kind,
    decimal? Balance,
    DateOnly? BalanceAsOf,
    decimal? Apr,
    decimal? MinimumPayment,
    DateOnly? NextDueDate,
    decimal? CreditLimit,
    int? RemainingTermMonths,
    decimal? PromotionalApr,
    DateOnly? PromotionalEndsOn,
    DebtLinkedBalance? LinkedBalance,
    bool Following = false,
    DebtLinkFreshness? Freshness = null);
