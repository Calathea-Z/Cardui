namespace Cardui.Api.Dtos.Transaction;

public class MerchantHistoryPeriodDto
{
    public required string Key { get; set; }
    public required string Label { get; set; }
    public required string ShortLabel { get; set; }
    public decimal TotalAmount { get; set; }
    public int TransactionCount { get; set; }
}
