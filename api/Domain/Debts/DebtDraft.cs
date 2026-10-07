using Cardui.Api.Models;

namespace Cardui.Api.Domain.Debts;

/// <summary>
/// One valid debt after the name, type, and terms are checked.
/// A null balance, APR, minimum, due date, limit, term, or promotion is unknown.
/// Zero is a known zero, not a stand-in for unknown.
/// AccountId is null when the debt is not linked to a household account.
/// CreditLimit is set only for a revolving debt. RemainingTermMonths is set only for an installment debt.
/// </summary>
public readonly record struct DebtDraft(
    string Name,
    DebtKind Kind,
    Guid? AccountId,
    decimal? Balance,
    DateOnly? BalanceAsOf,
    decimal? Apr,
    decimal? MinimumPayment,
    DateOnly? NextDueDate,
    decimal? CreditLimit,
    int? RemainingTermMonths,
    decimal? PromotionalApr,
    DateOnly? PromotionalEndsOn);
