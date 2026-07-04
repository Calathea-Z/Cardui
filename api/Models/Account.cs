namespace Cardui.Api.Models;

public class Account
{
    public Guid Id { get; set; }

    public Guid PlaidItemId { get; set; }
    public PlaidItem PlaidItem { get; set; } = null!;

    public required string PlaidAccountId { get; set; }

    public required string Name { get; set; }
    public string? OfficialName { get; set; }

    public required string Type { get; set; }
    public string? Subtype { get; set; }
    public string? Mask { get; set; }

    public decimal CurrentBalance { get; set; }
    public decimal? AvailableBalance { get; set; }

    public string? IsoCurrencyCode { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}