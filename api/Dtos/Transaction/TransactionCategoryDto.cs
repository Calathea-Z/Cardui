namespace Cardui.Api.Dtos.Transaction;

public class TransactionCategoryDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Color { get; set; }
    public string? Icon { get; set; }
}