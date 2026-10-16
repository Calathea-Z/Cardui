using Cardui.Api.Domain.Recovery;
using Cardui.Api.Dtos.Plan;

namespace Cardui.Api.Services.Interfaces;

public interface ISavedPlanService
{
    /// <summary>
    /// Builds the decision plan for the current facts and the selected scenario.
    /// Nothing is saved. Kind is roll payments forward or free up cash.
    /// </summary>
    Task<DecisionPlanDto> GetPreviewAsync(
        PlanScenarioQuery scenario,
        PayoffRolloverKind kind,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists the household's saved plan versions, newest first.
    /// Each version keeps the decision plan from the day it was saved.
    /// </summary>
    Task<IReadOnlyList<PlanVersionDto>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns one saved plan version for this household.
    /// </summary>
    Task<PlanVersionDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the current decision plan as the open draft.
    /// A repeated save of the same plan returns the open draft. A request that names a different draft is rejected.
    /// Replacing a draft marks that draft superseded. Nothing is accepted, and no money is moved.
    /// </summary>
    Task<PlanVersionDto> SaveDraftAsync(
        SavePlanVersionDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Accepts the open draft and marks the previous accepted plan superseded.
    /// Accepting the plan that is already accepted returns it. Any other version is rejected.
    /// The accepted copy does not change when later edits are saved. No money is moved.
    /// </summary>
    Task<PlanVersionDto> AcceptAsync(Guid id, CancellationToken cancellationToken = default);
}
