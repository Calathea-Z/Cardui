namespace Cardui.Api.Domain.Planning;

/// <summary>
/// One transaction description or uploaded excerpt proposed for a share.
/// It is quoted as data. It is not copied into the developer instructions.
/// </summary>
public sealed record PlanningUntrustedText(PlanningShareTextKind Kind, string Text);
