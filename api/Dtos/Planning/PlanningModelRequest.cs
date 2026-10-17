namespace Cardui.Api.Dtos.Planning;

/// <summary>
/// One planning-model turn. A null Instructions value sends no developer text.
/// </summary>
public sealed record PlanningModelRequest(string Input, string? Instructions);
