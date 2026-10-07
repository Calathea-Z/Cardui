namespace Cardui.Api.Dtos.Plaid;

public class SyncPlaidItemResponseDto
{
    public Guid PlaidItemId { get; set; }
    public SyncTransactionsResponseDto Transactions { get; set; } = new();

    /// <summary>
    /// True when this call did not sync because another sync still holds the item.
    /// Transaction counts stay zero, and the item's accounts and timestamps are unchanged.
    /// </summary>
    public bool AlreadyRunning { get; set; }
}