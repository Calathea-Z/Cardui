using Cardui.Api.Dtos.Planning;

namespace Cardui.Api.Services.Interfaces;

public interface IPlanningCalls
{
    /// <summary>
    /// Asks the planning model once, within the timeout, retry, and size limits.
    /// A provider failure, a missing configuration, or a text past the character cap leaves the conversation and the open draft unchanged.
    /// The result then says the person can continue on Plan. The reply is not stored. Nothing financial is written, and no money is moved.
    /// A bank access token is refused before the model is called.
    /// </summary>
    Task<PlanningCallDto> AskAsync(
        Guid conversationId,
        PlanningModelRequest request,
        CancellationToken cancellationToken = default);
}
