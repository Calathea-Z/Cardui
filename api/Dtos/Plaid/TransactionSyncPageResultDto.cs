namespace Cardui.Api.Dtos.Plaid;

public sealed class TransactionSyncPageResultDto
{
    public int Added { get; init; }
    public int Modified { get; init; }
    public int Removed { get; init; }
}
