namespace Cardui.Api.Domain.Debts;

/// <summary>
/// How current a followed debt's connection is.
/// Current means the latest snapshot is recent and the last sync succeeded.
/// The other states keep showing the last balance.
/// </summary>
public enum DebtLinkFreshness
{
    Current,
    Stale,
    SyncFailing,
    Disconnected,
    AccountMissing
}
