using Cardui.Api.Dtos.Plaid;
using Going.Plaid.Entity;
using Account = Cardui.Api.Models.Account;

namespace Cardui.Api.Services.Plaid;

public interface IPlaidTransactionReconciler
{
    /// <summary>
    /// Inserts and updates stored transactions from one sync batch, promotes
    /// a pending row when Plaid posts it, and deletes rows Plaid removed.
    /// A user-edited date is kept. A user-edited category is not replaced.
    /// </summary>
    Task<TransactionSyncPageResultDto> ReconcileAsync(
        Guid plaidItemId,
        IReadOnlyDictionary<string, Account> accountsByPlaidId,
        IReadOnlyCollection<Transaction> added,
        IReadOnlyCollection<Transaction> modified,
        IReadOnlyCollection<RemovedTransaction> removed,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);
}
