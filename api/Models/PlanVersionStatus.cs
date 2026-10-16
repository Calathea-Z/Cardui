namespace Cardui.Api.Models;

/// <summary>
/// Where a saved plan is in its history.
/// A household has at most one draft and one accepted plan. Older copies are superseded.
/// The stored column and the API use the member name.
/// </summary>
public enum PlanVersionStatus
{
    Draft,
    Accepted,
    Superseded
}
