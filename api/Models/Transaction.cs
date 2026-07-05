namespace Cardui.Api.Models;

public class Transaction
{
    public Guid Id { get; init; }

    public Guid AccountId { get; set; }
    public Account Account { get; init; } = null!;

    public required string PlaidTransactionId { get; init; }

    public DateOnly Date { get; set; }
    public DateOnly? AuthorizedDate { get; set; }

    public required string Name { get; set; }
    public string? MerchantName { get; set; }

    public decimal Amount { get; set; }

    public string? IsoCurrencyCode { get; set; }
    public bool Pending { get; set; }

    public Guid? CategoryId { get; set; }
    public Category? Category { get; init; }

    public string? Notes { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; set; }
}