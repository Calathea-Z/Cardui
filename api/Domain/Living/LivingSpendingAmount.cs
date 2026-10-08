namespace Cardui.Api.Domain.Living;

/// <summary>
/// One monthly flexible-spending amount considered by the Living affordability rule.
/// Another currency is named and left out.
/// </summary>
public sealed record LivingSpendingAmount(
    string Name,
    decimal MonthlyAmount,
    string Currency);
