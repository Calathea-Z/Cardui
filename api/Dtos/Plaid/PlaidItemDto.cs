namespace Cardui.Api.Dtos.Plaid;

public class PlaidItemDto
{
    public Guid Id { get; set; }
    public string? InstitutionId { get; set; }
    public string? InstitutionName { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? LastTransactionsSyncedAt { get; set; }
    public DateTimeOffset? LastSyncStartedAt { get; set; }
    public DateTimeOffset? LastSyncCompletedAt { get; set; }
    public DateTimeOffset? LastSyncFailedAt { get; set; }
    public string? LastSyncError { get; set; }

    /// <summary>
    /// True when the latest sync failed after the last success, or failed
    /// without one. The connection can be repaired in Plaid Link.
    /// </summary>
    public bool NeedsRepair { get; set; }
}