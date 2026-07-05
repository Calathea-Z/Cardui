namespace Cardui.Api.Dtos.Transaction;

public class TransactionQueryDto
{
    public string? Search { get; set; }
    public Guid? AccountId { get; set; }
    public Guid? CategoryId { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
    public bool? Pending { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}