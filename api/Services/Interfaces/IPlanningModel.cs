using Cardui.Api.Dtos.Planning;

namespace Cardui.Api.Services.Interfaces;

public interface IPlanningModel
{
    /// <summary>
    /// Asks the configured planning model for one text reply.
    /// The call has no tools. Cardui does not keep the exchange.
    /// </summary>
    Task<PlanningModelReply> ReplyAsync(
        PlanningModelRequest request,
        CancellationToken cancellationToken);
}
