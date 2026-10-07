namespace Cardui.Api.Domain;

/// <summary>
/// The name and color of a category that had spending in a month.
/// </summary>
internal sealed record ActivityCategoryLabel(string Name, string? Color);
