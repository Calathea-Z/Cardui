namespace Cardui.Api.Domain.Plans;

/// <summary>
/// What a save request is allowed to do to the open draft.
/// Unchanged means the open draft already has this plan. Stale means the request named a draft that is not the open one.
/// </summary>
public enum PlanVersionSaveResult
{
    Create,
    ReplaceDraft,
    Unchanged,
    Stale
}
