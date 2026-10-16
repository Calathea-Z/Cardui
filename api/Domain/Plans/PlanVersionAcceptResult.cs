namespace Cardui.Api.Domain.Plans;

/// <summary>
/// What an accept request is allowed to do.
/// AlreadyAccepted means this version is the accepted plan, including a repeated request. Stale means it is not the open draft.
/// </summary>
public enum PlanVersionAcceptResult
{
    Accept,
    AlreadyAccepted,
    Stale
}
