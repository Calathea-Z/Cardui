using PlaidTransaction = Going.Plaid.Entity.Transaction;

namespace Cardui.Api.Services.Interfaces;

public interface ITransactionCategorizationService
{
    Task<Guid?> GetCategoryIdForPlaidTransactionAsync(PlaidTransaction transaction);
}