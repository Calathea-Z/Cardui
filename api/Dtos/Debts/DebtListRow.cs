using Cardui.Api.Models;

namespace Cardui.Api.Dtos.Debts;

/// <summary>
/// One debt row before its followed balance is resolved.
/// Balance is the amount stored on the debt. It is not yet the amount in use.
/// </summary>
internal sealed record DebtListRow(
    Guid Id,
    string Name,
    DebtKind Kind,
    Guid? AccountId,
    string? AccountName,
    decimal? Balance,
    DateOnly? BalanceAsOf,
    string Currency,
    decimal? Apr,
    decimal? MinimumPayment,
    DateOnly? NextDueDate,
    decimal? CreditLimit,
    int? RemainingTermMonths,
    decimal? PromotionalApr,
    DateOnly? PromotionalEndsOn,
    DateTimeOffset? AccountFollowedSince,
    DateTimeOffset? BalanceOverriddenAt);
