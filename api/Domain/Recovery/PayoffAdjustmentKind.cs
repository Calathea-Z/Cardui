namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// A reason the recommended order might leave avalanche.
/// MinimumRelease pays one debt off sooner. Utilization brings one revolving debt under 90 percent of its limit.
/// Constraint honors a pay-first or pay-last selection.
/// The stored text is the member name.
/// </summary>
public enum PayoffAdjustmentKind
{
    MinimumRelease,
    Utilization,
    Constraint
}
