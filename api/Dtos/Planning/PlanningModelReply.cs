namespace Cardui.Api.Dtos.Planning;

/// <summary>
/// The planning model's text for one turn.
/// Tortoise does not keep the exchange, and the text is not a calculated forecast.
/// </summary>
public sealed record PlanningModelReply(string Text);
