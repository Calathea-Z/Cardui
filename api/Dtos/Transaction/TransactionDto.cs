namespace Cardui.Api.Dtos.Transaction;
public class TransactionDto
{
    public Guid Id { get; set; }
    public DateOnly Date { get; set; }
    public DateOnly? AuthorizedDate { get; set; }
    public required string Name { get; set; }
    public string? MerchantName { get; set; }
    public decimal Amount { get; set; }
    public string? IsoCurrencyCode { get; set; }
    public bool Pending { get; set; }
    public required TransactionAccountDto Account { get; set; }
    public TransactionCategoryDto? Category { get; set; }
}