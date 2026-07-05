namespace Cardui.Api.Dtos.Plaid;

public class SyncPlaidItemResponseDto
{
    public Guid PlaidItemId { get; set; }
    public SyncTransactionsResponseDto Transactions { get; set; } = new();
}