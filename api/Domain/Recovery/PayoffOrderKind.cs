namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// Which ranking a payoff order is.
/// Avalanche is the highest rate on the first due date. Recommended may leave that order.
/// UserSelected is the caller's ranking, including when it matches avalanche because none was sent.
/// The stored text is the member name.
/// </summary>
public enum PayoffOrderKind
{
    Avalanche,
    Recommended,
    UserSelected
}
