namespace Cardui.Api.Dtos.Transaction;

public class MerchantHistoryPeriodDto
{
    public required string Key { get; set; }
    public required string Label { get; set; }
    public required string ShortLabel { get; set; }
    public decimal TotalAmount { get; set; }
    public int TransactionCount { get; set; }
}

public class MerchantHistoryDto
{
    public required string DisplayName { get; set; }
    public int TotalTransactionCount { get; set; }
    public required string Granularity { get; set; }
    public required string SelectedPeriodKey { get; set; }
    public required IReadOnlyList<MerchantHistoryPeriodDto> Periods { get; set; }
    public required IReadOnlyList<TransactionDto> Transactions { get; set; }
}
