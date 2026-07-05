namespace Cardui.Api.Models;

public class PlaidItem
{
    public Guid Id { get; init; }

    public required string PlaidItemId { get; init; }
    public required string AccessToken { get; init; }

    public string? InstitutionId { get; init; }
    public string? InstitutionName { get; init; }

    public string? TransactionsCursor { get; set; }
    public DateTimeOffset? LastTransactionsSyncedAt { get; set; }

    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<Account> Accounts { get; init; } = new List<Account>();
}