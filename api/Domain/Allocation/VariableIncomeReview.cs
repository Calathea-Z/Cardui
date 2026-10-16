namespace Cardui.Api.Domain.Allocation;

/// <summary>
/// One variable source prepared for review.
/// Surplus is the amount above typical pay, in the source currency. It is null when the source is left out.
/// LeftOut is null when Surplus is set. The other values are the constants on the allocation rules.
/// </summary>
public sealed record VariableIncomeReview(
    string Name,
    string Currency,
    decimal? Surplus,
    string? LeftOut);
