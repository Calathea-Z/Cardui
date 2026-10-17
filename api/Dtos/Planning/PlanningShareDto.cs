using Cardui.Api.Domain.Planning;

namespace Cardui.Api.Dtos.Planning;

/// <summary>
/// What may be sent to the planning model for one conversation.
/// Sendable is false when sharing is not allowed or nothing safe remains. Input and Instructions are then null.
/// Omissions name what was left out. They do not include a bank token or the omitted text.
/// </summary>
public sealed class PlanningShareDto
{
    public PlanningShareConsent Consent { get; init; }

    public bool Sendable { get; init; }

    public string? Input { get; init; }

    public string? Instructions { get; init; }

    public IReadOnlyList<PlanningShareSection> Included { get; init; } = [];

    public IReadOnlyList<string> Omissions { get; init; } = [];
}
