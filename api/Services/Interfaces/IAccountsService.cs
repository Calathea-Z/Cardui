using Cardui.Api.Dtos.Account;

namespace Cardui.Api.Services.Interfaces;

public interface IAccountsService
{
    Task<IReadOnlyList<AccountDto>> GetAccountsAsync(
        bool includeArchived = false,
        CancellationToken cancellationToken = default);

    Task<AccountSummaryDto> GetAccountsSummaryAsync(
        CancellationToken cancellationToken = default);

    Task<AccountDto> CreateManualAccountAsync(
        CreateManualAccountDto dto,
        CancellationToken cancellationToken = default);

    Task<AccountDto> UpdateManualAccountAsync(
        Guid accountId,
        UpdateManualAccountDto dto,
        CancellationToken cancellationToken = default);

    Task<AccountDto> ArchiveAccountAsync(
        Guid accountId,
        CancellationToken cancellationToken = default);

    Task<AccountDto> RestoreAccountAsync(
        Guid accountId,
        CancellationToken cancellationToken = default);

    Task<BalanceReconciliationResultDto> ReconcileBalanceAsync(
        Guid accountId,
        ReconcileAccountBalanceDto dto,
        CancellationToken cancellationToken = default);
}
