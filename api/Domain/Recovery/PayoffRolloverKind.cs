namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// Which way a projection treats cash freed by a paid-off debt.
/// Rollover sends the freed minimum and planned extra to the next debt. That is the default.
/// Reclaim keeps a chosen amount of that cash each month for savings or spending and rolls the rest.
/// ReclaimAll keeps every freed dollar out of the next debt.
/// The stored text is the member name.
/// </summary>
public enum PayoffRolloverKind
{
    Rollover,
    Reclaim,
    ReclaimAll
}
