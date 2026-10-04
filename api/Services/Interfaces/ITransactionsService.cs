using Cardui.Api.Dtos.Common;
using Cardui.Api.Dtos.Transaction;

namespace Cardui.Api.Services.Interfaces;

public interface ITransactionsService
{
    /// <summary>
    /// Returns one page of the household's transactions. Search, account,
    /// category, date, pending, and archived filters come from the query.
    /// Archived rows are omitted unless the query asks for them.
    /// </summary>
    Task<PagedResultDto<TransactionDto>> GetTransactionsAsync(
        TransactionQueryDto query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns one household transaction, including its account and category.
    /// </summary>
    Task<TransactionDto> GetTransactionByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Groups this household's activity for the same merchant.
    /// Granularity is monthly, quarterly, or yearly. Anything else stays monthly.
    /// </summary>
    Task<MerchantHistoryDto> GetMerchantHistoryAsync(
        Guid transactionId,
        string granularity = "monthly",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the transaction's category and marks it user-edited so a later
    /// Plaid sync does not replace the choice.
    /// </summary>
    Task<TransactionDto> UpdateTransactionCategoryAsync(
        Guid transactionId,
        UpdateTransactionCategoryDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the date, category, and notes. Name, amount, and pending
    /// change only on a manual entry. A manual account's balance is recalculated.
    /// </summary>
    Task<TransactionDto> UpdateTransactionDetailsAsync(
        Guid transactionId,
        UpdateTransactionDetailsDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a manual transaction and recalculates the balance when that
    /// account uses the ledger. Dates before the opening date, or in the
    /// future, are rejected.
    /// </summary>
    Task<TransactionDto> CreateManualTransactionAsync(
        CreateManualTransactionDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Hides a transaction from activity and recalculates a manual account's balance.
    /// </summary>
    Task<TransactionDto> ArchiveTransactionAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears ArchivedAt and recalculates a manual account's balance.
    /// </summary>
    Task<TransactionDto> RestoreTransactionAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default);
}
