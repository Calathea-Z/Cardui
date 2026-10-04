namespace Cardui.Api.Dtos.Transaction;

public class MerchantHistoryDto
{
    public required string DisplayName { get; set; }
    public int TotalTransactionCount { get; set; }
    public required string Granularity { get; set; }
    public required string SelectedPeriodKey { get; set; }
    public required IReadOnlyList<MerchantHistoryPeriodDto> Periods { get; set; }
    public required IReadOnlyList<TransactionDto> Transactions { get; set; }
}
