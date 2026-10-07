namespace Cardui.Api.Domain.Transactions;

/// <summary>
/// Groups spending by category id, name, and color.
/// </summary>
internal sealed record CategoryBucket(
    Guid? CategoryId,
    string CategoryName,
    string? CategoryColor);
