using Cardui.Api.Dtos.Plaid;
using Going.Plaid.Entity;
using Account = Cardui.Api.Models.Account;

namespace Cardui.Api.Services.Plaid;

public interface IPlaidTransactionReconciler
{
    Task<TransactionSyncPageResultDto> ReconcileAsync(
        Guid plaidItemId,
        IReadOnlyDictionary<string, Account> accountsByPlaidId,
        IReadOnlyCollection<Transaction> added,
        IReadOnlyCollection<Transaction> modified,
        IReadOnlyCollection<RemovedTransaction> removed,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);
}
