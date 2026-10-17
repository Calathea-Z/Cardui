namespace Cardui.Api.Domain.Planning;

/// <summary>
/// Why a reviewed proposal can no longer be accepted.
/// Conversation means the thread moved or the review names a different one.
/// Draft and Accepted mean a plan version changed outside this review.
/// Plan means the calculated document changed, including an edit to pay, bills, or debts.
/// The member name is the JSON text. This value is not stored.
/// </summary>
public enum PlanRevisionStaleReason
{
    Conversation,
    Draft,
    Accepted,
    Plan
}
