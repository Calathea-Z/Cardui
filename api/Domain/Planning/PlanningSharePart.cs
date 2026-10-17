namespace Cardui.Api.Domain.Planning;

/// <summary>
/// One line of a household section proposed for a share.
/// A line that contains a bank access token is left out. The other lines in that section can remain.
/// </summary>
public sealed record PlanningSharePart(PlanningShareSection Section, string Text);
