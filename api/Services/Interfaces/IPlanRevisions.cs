using Cardui.Api.Domain.Recovery;
using Cardui.Api.Dtos.Plan;
using Cardui.Api.Dtos.Planning;

namespace Cardui.Api.Services.Interfaces;

public interface IPlanRevisions
{
    /// <summary>
    /// Shows what accepting this scenario would change in the kept plan.
    /// The numbers come from the plan calculation. Nothing is saved or accepted, and no money is moved.
    /// A review token that does not match the conversation or the plan versions is stale and cannot be accepted.
    /// </summary>
    Task<PlanRevisionReviewDto> ReviewAsync(
        Guid conversationId,
        PlanScenarioQuery scenario,
        PayoffRolloverKind kind,
        string? reviewToken = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Accepts the proposal from a current review.
    /// The conversation, the open draft, the accepted plan, and the calculated plan all have to match that review.
    /// A conversation that moved is rejected even when the calculated plan already matches.
    /// A repeated accept of the same plan returns it when the conversation has not moved. An outside edit is rejected.
    /// No money is moved.
    /// </summary>
    Task<PlanVersionDto> AcceptAsync(
        Guid conversationId,
        PlanScenarioQuery scenario,
        PayoffRolloverKind kind,
        string reviewToken,
        CancellationToken cancellationToken = default);
}
