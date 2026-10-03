using Going.Plaid.Entity;
using PlaidTransaction = Going.Plaid.Entity.Transaction;

namespace Cardui.Api.Dtos.Plaid;

public sealed class PlaidTransactionPageDto
{
    public IReadOnlyList<PlaidTransaction> Added { get; init; } = [];
    public IReadOnlyList<PlaidTransaction> Modified { get; init; } = [];
    public IReadOnlyList<RemovedTransaction> Removed { get; init; } = [];
    public string? NextCursor { get; init; }
    public bool HasMore { get; init; }
}
