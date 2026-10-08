namespace Cardui.Api.Models;

/// <summary>
/// Which savings row this is.
/// A household has one everyday-spending plan, one cash floor, and one emergency goal. Named goals repeat.
/// </summary>
public enum SavingsGoalKind
{
    Operating,
    Floor,
    Emergency,
    Sinking
}
