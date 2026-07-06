namespace Cardui.Api.Models;

public class Account
{
    public Guid Id { get; init; }

    public Guid PlaidItemId { get; init; }
    public PlaidItem PlaidItem { get; init; } = null!;

    public required string PlaidAccountId { get; init; }

    public required string Name { get; set; }
    public string? OfficialName { get; set; }

    public required string Type { get; set; }
    public string? Subtype { get; set; }
    public string? Mask { get; set; }

    public decimal CurrentBalance { get; set; }
    public decimal? AvailableBalance { get; set; }

    public string? IsoCurrencyCode { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<Transaction> Transactions { get; init; } = new List<Transaction>();

    public ICollection<AccountBalanceSnapshot> BalanceSnapshots { get; init; } =
        new List<AccountBalanceSnapshot>();
}