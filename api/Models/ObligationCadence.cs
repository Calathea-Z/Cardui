namespace Cardui.Api.Models;

/// <summary>
/// How often a bill is due.
/// The stored column and the API use the member name.
/// Biweekly is every two weeks. Semimonthly is twice a month.
/// Those are different schedules.
/// </summary>
public enum ObligationCadence
{
    Weekly,
    Biweekly,
    Semimonthly,
    Monthly,
    Quarterly,
    Yearly,
    Irregular
}
