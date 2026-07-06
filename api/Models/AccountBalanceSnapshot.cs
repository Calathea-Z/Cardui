namespace Cardui.Api.Models;

public class AccountBalanceSnapshot
{
    public Guid Id { get; init; }

    public Guid AccountId { get; init; }
    public Account Account { get; init; } = null!;

    public DateOnly Date { get; init; }

    public decimal CurrentBalance { get; init; }
    public decimal? AvailableBalance { get; init; }

    public string? IsoCurrencyCode { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}