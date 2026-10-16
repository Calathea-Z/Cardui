namespace Cardui.Api.Domain.Plans;

/// <summary>
/// Decides whether a save or accept request matches the open draft.
/// The database still enforces one draft and one accepted plan. This rule is the decision before that write.
/// </summary>
public static class PlanVersionRules
{
    /// <summary>
    /// Decides whether to create a draft, replace the open draft, keep it, or reject the request.
    /// A missing open draft accepts only a request that does not name one. The same document does not create another version.
    /// An expected id that is not the open draft is stale, including a request that names a draft when none is open.
    /// </summary>
    public static PlanVersionSaveResult Save(
        Guid? openDraftId,
        Guid? replacesDraftId,
        bool sameDocument)
    {
        if (openDraftId is null)
        {
            return replacesDraftId is null
                ? PlanVersionSaveResult.Create
                : PlanVersionSaveResult.Stale;
        }

        if (replacesDraftId != openDraftId)
        {
            return PlanVersionSaveResult.Stale;
        }

        return sameDocument
            ? PlanVersionSaveResult.Unchanged
            : PlanVersionSaveResult.ReplaceDraft;
    }

    /// <summary>
    /// Decides whether accepting this version is allowed.
    /// A version that is already accepted stays accepted, including a repeated request.
    /// Any other version that is not the open draft is stale.
    /// </summary>
    public static PlanVersionAcceptResult Accept(bool isOpenDraft, bool isThisAccepted)
    {
        if (isThisAccepted)
        {
            return PlanVersionAcceptResult.AlreadyAccepted;
        }

        return isOpenDraft
            ? PlanVersionAcceptResult.Accept
            : PlanVersionAcceptResult.Stale;
    }
}
