using Cardui.Api.Models;

namespace Cardui.Api.Domain.Planning;

/// <summary>
/// One paycheck the question rule can see.
/// Reliability says whether it can be counted as cash that will arrive. Steady can. Variable and Uncertain cannot.
/// </summary>
public sealed record PlanningIncomeFact(
    Guid Id,
    string Name,
    IncomeReliability Reliability,
    DateOnly NextPaymentDate);
