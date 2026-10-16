namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// Whether the offer's running cost, including its fee, stays at or below keeping the debts.
/// Reached names the first date that remains true through both payoffs.
/// Never means both paths pay off and the offer stays more expensive. Unknown means a path does not pay off.
/// </summary>
public enum RestructuringBreakEven
{
    Reached,
    Never,
    Unknown
}
