using Cardui.Api.Dtos.Account;

namespace Cardui.Api.Services.Interfaces;

public interface IAccountsService
{
    /// <summary>
    /// Lists the signed-in household's accounts, ordered by name.
    /// Archived accounts are omitted unless includeArchived is true.
    /// </summary>
    Task<IReadOnlyList<AccountDto>> GetAccountsAsync(
        bool includeArchived = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds the accounts summary: net worth, balances by type, daily
    /// history, and archived accounts. Inactive and archived accounts stay
    /// out of the totals.
    /// </summary>
    Task<AccountSummaryDto> GetAccountsSummaryAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a manual account from an opening balance. That balance is
    /// the starting point, not a transaction, and today's snapshot is stored.
    /// </summary>
    Task<AccountDto> CreateManualAccountAsync(
        CreateManualAccountDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a manual account's name, type, and opening balance, then
    /// recalculates the current balance. Linked accounts are rejected.
    /// </summary>
    Task<AccountDto> UpdateManualAccountAsync(
        Guid accountId,
        UpdateManualAccountDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Hides an account from totals by setting ArchivedAt. Its transactions
    /// stay in activity until those transactions are archived.
    /// </summary>
    Task<AccountDto> ArchiveAccountAsync(
        Guid accountId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears ArchivedAt so the account is included in totals again.
    /// </summary>
    Task<AccountDto> RestoreAccountAsync(
        Guid accountId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Compares a manual account's calculated balance with a statement.
    /// A difference is saved as a balance-reconciliation transaction, which
    /// income, spending, and transfer pairing ignore.
    /// </summary>
    Task<BalanceReconciliationResultDto> ReconcileBalanceAsync(
        Guid accountId,
        ReconcileAccountBalanceDto dto,
        CancellationToken cancellationToken = default);
}
