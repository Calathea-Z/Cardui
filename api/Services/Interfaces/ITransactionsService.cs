using Cardui.Api.Dtos.Common;
using Cardui.Api.Dtos.Transaction;

namespace Cardui.Api.Services.Interfaces;

public interface ITransactionsService
{
    Task<PagedResultDto<TransactionDto>> GetTransactionsAsync(TransactionQueryDto query);
    Task<TransactionDto> GetTransactionByIdAsync(Guid id);
    Task<TransactionDto> UpdateTransactionCategoryAsync(
        Guid transactionId,
        UpdateTransactionCategoryDto dto);
}
