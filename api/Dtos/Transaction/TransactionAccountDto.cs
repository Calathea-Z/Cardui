namespace Cardui.Api.Dtos.Transaction;

public class TransactionAccountDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Type { get; set; }
    public string? Subtype { get; set; }
}