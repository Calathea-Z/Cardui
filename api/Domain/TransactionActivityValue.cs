namespace Cardui.Api.Domain;

/// <summary>
/// One transaction reduced to the fields income and spending need.
/// </summary>
public sealed record TransactionActivityValue(
    decimal Amount,
    bool Pending,
    Guid? CategoryId,
    string CategoryName,
    string? CategoryColor,
    string? CategoryKey,
    string? GroupKey,
    string? Provenance = null);
