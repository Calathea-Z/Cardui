namespace Cardui.Api.Dtos.Plaid;

public class PlaidItemDto
{
    public Guid Id { get; set; }
    public string? InstitutionId { get; set; }
    public string? InstitutionName { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? LastTransactionsSyncedAt { get; set; }
}