using Cardui.Api.Dtos.Common;
using Cardui.Api.Dtos.Transaction;

namespace Cardui.Api.Services.Interfaces;

public interface ITransactionsService
{
    Task<PagedResultDto<TransactionDto>> GetTransactionsAsync(
        TransactionQueryDto query,
        CancellationToken cancellationToken = default);

    Task<TransactionDto> GetTransactionByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<MerchantHistoryDto> GetMerchantHistoryAsync(
        Guid transactionId,
        string granularity = "monthly",
        CancellationToken cancellationToken = default);

    Task<TransactionDto> UpdateTransactionCategoryAsync(
        Guid transactionId,
        UpdateTransactionCategoryDto dto,
        CancellationToken cancellationToken = default);

    Task<TransactionDto> UpdateTransactionDetailsAsync(
        Guid transactionId,
        UpdateTransactionDetailsDto dto,
        CancellationToken cancellationToken = default);
}
