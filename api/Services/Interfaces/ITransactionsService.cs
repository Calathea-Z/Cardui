using Cardui.Api.Dtos.Transaction;

namespace Cardui.Api.Services.Interfaces;

public interface ITransactionsService
{
    Task<IReadOnlyList<TransactionDto>> GetTransactionsAsync(TransactionQueryDto  query);
    Task<TransactionDto?> GetTransactionByIdAsync(Guid id);
    Task<TransactionDto?> UpdateTransactionCategoryAsync(Guid transactionId, UpdateTransactionCategoryDto dto);
}