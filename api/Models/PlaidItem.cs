namespace Cardui.Api.Models;

public class PlaidItem
{
    public Guid Id { get; set; }

    public required string PlaidItemId { get; set; }
    public required string AccessToken { get; set; }

    public string? InstitutionId { get; set; }
    public string? InstitutionName { get; set; }

    public string? TransactionsCursor { get; set; }
    public DateTimeOffset? LastTransactionsSyncedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<Account> Accounts { get; set; } = new List<Account>();
}