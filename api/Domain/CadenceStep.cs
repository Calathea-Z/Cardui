namespace Cardui.Api.Domain;

/// <summary>
/// How a dated amount repeats.
/// Weekly and biweekly step by days. Semimonthly is two days in the month.
/// Monthly, quarterly, and yearly step by months from the original anchor.
/// Irregular is not a step, because it has no repeat.
/// </summary>
public enum CadenceStep
{
    Weekly,
    Biweekly,
    Semimonthly,
    Monthly,
    Quarterly,
    Yearly
}
