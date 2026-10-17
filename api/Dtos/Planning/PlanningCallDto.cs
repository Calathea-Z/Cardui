using Cardui.Api.Domain.Planning;

namespace Cardui.Api.Dtos.Planning;

/// <summary>
/// The outcome of one guarded planning-model call.
/// Text is null when the person should continue on Plan. Notice is null when the provider answered.
/// DraftId is the open draft, or null when the household has none. The call does not save the reply or replace that draft.
/// </summary>
public sealed class PlanningCallDto
{
    public PlanningCallResult Result { get; init; }

    public string? Text { get; init; }

    public string? Notice { get; init; }

    public Guid? DraftId { get; init; }
}
