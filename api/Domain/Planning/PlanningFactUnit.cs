namespace Cardui.Api.Domain.Planning;

/// <summary>
/// What an amount on a planning fact means.
/// Money is a currency amount. Percent is a rate, not dollars.
/// None means the fact is a date, a note, or a suggested account, so it has no amount.
/// The member name is the JSON text. This value is not stored.
/// </summary>
public enum PlanningFactUnit
{
    Money,
    Percent,
    None
}
