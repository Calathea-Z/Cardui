using Cardui.Api.Dtos.Planning;

namespace Cardui.Api.Services.Interfaces;

public interface IPlanningModel
{
    /// <summary>
    /// Asks the configured planning model for one text reply.
    /// The call has no tools. Tortoise does not keep the exchange.
    /// A bank access token is refused before the request is sent.
    /// One attempt stops at the timeout. A transient failure is tried again up to the attempt limit.
    /// Text past the character cap is not sent, and the request caps output tokens. No price is set.
    /// </summary>
    Task<PlanningModelReply> ReplyAsync(
        PlanningModelRequest request,
        CancellationToken cancellationToken);
}
