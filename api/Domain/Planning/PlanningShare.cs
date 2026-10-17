namespace Cardui.Api.Domain.Planning;

/// <summary>
/// The text that may be sent to the planning model, or a refusal.
/// Sendable is false when sharing is not allowed, the message contains a bank token, or nothing safe remains.
/// Input and Instructions are null in that case. Omissions do not repeat the left-out text.
/// </summary>
public sealed record PlanningShare(
    PlanningShareConsent Consent,
    bool Sendable,
    string? Input,
    string? Instructions,
    IReadOnlyList<PlanningShareSection> Included,
    IReadOnlyList<string> Omissions);
