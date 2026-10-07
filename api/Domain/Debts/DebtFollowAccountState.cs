using Cardui.Api.Models;

namespace Cardui.Api.Domain.Debts;

/// <summary>
/// The account values a followed balance reads, plus the names used to suggest a match.
/// Balance is the latest snapshot, or the current balance when no snapshot has a date.
/// HasPlaidItem is false after the bank link is removed.
/// OfficialName and InstitutionName are null when the connection did not provide them.
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
    DateTimeOffset? LastSyncFailedAt,
    string? OfficialName,
    string? InstitutionName);