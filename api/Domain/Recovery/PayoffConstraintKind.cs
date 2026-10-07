namespace Cardui.Api.Domain.Recovery;

/// <summary>
/// How a selected constraint moves one debt in the recommended order.
/// PayFirst puts it ahead of avalanche. PayLast puts it after the other debts that can be paid.
/// The stored text is the member name.
/// </summary>
public enum PayoffConstraintKind
{
    PayFirst,
    PayLast
}
