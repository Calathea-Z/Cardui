using Cardui.Api.Domain.Debts;
using Cardui.Api.Models;

namespace Cardui.Api.Domain;

/// <summary>
/// The account values a followed balance reads.
/// Balance is the latest snapshot, or the current balance when no snapshot has a date.
/// HasPlaidItem is false after the bank link is removed.
/// </summary>
internal sealed record DebtFollowAccountState(
    Guid Id,
    string Name,
    string? Mask,
    FinancialRecordSource Source,
    bool HasPlaidItem,
    bool IsActive,
    bool IsArchived,
    string Type,
    string? Currency,
    DebtLinkedBalance? Balance,
    DateTimeOffset? LastSyncCompletedAt,
    DateTimeOffset? LastSyncFailedAt);