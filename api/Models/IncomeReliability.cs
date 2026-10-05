namespace Cardui.Api.Models;

/// <summary>
/// How dependable an income source is.
/// Steady is expected in full on schedule. Variable can change in amount
/// or timing. Uncertain may not arrive. The stored column and the API
/// use the member name.
/// </summary>
public enum IncomeReliability
{
    Steady,
    Variable,
    Uncertain
}
