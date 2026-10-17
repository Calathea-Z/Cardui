using Cardui.Api.Domain.Planning;

namespace Cardui.Api.Dtos.Planning;

/// <summary>
/// The pieces a caller wants in one planning-model request.
/// Message is the person's own words. Sections are household summaries.
/// Untrusted text is one transaction description or uploaded excerpt, and it is data.
/// Nothing here is sent until sharing is allowed and the share is prepared.
/// </summary>
public sealed class PlanningShareRequest
{
    public string? Message { get; set; }

    public IReadOnlyList<PlanningShareSection> Sections { get; set; } = [];

    public string? UntrustedText { get; set; }

    public PlanningShareTextKind? UntrustedKind { get; set; }
}
