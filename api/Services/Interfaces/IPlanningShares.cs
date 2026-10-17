using Cardui.Api.Dtos.Planning;

namespace Cardui.Api.Services.Interfaces;

public interface IPlanningShares
{
    /// <summary>
    /// Returns whether this conversation may include household data in a planning-model request.
    /// Not asked and declined both mean it may not. A closed conversation can still be read.
    /// </summary>
    Task<PlanningShareConsentDto> GetConsentAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records an explicit yes for this open conversation.
    /// A later conversation does not copy it. Connecting a bank or importing a file does not call this.
    /// </summary>
    Task<PlanningShareConsentDto> AllowAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records an explicit no for this open conversation.
    /// Later shares include no household data until the person allows it again.
    /// </summary>
    Task<PlanningShareConsentDto> DeclineAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds the text that may be sent to the planning model.
    /// Household sections are included only after an explicit yes, and only the sections named here.
    /// A bank access token is never included. A transaction description or uploaded text stays in the data, not the instructions.
    /// The planning model is not called. Nothing financial is saved.
    /// </summary>
    Task<PlanningShareDto> PrepareAsync(
        Guid conversationId,
        PlanningShareRequest request,
        CancellationToken cancellationToken = default);
}
