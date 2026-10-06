using Cardui.Api.Models;

namespace Cardui.Api.Domain;

/// <summary>
/// One recorded debt, plus the linked account balance when there is one.
/// A null balance, rate, minimum, due date, limit, term, or promotion is unknown.
/// LinkedBalance is null when the debt is not linked to an account.
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
    DebtLinkedBalance? LinkedBalance);
