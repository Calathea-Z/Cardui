namespace Cardui.Api.Models;

public class TransactionImport
{
    public Guid Id { get; init; }

    public Guid HouseholdId { get; set; }

    public Guid AccountId { get; set; }
    public Account Account { get; init; } = null!;

    public required string FileName { get; set; }

    public int ImportedCount { get; set; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? UndoneAt { get; set; }

    public ICollection<Transaction> Transactions { get; init; } = new List<Transaction>();
}
