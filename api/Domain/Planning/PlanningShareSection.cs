namespace Cardui.Api.Domain.Planning;

/// <summary>
/// One household section that may be included in a planning-model request after consent.
/// The person's message and untrusted text are not sections. The member name is the JSON text.
/// This value is not stored.
/// </summary>
public enum PlanningShareSection
{
    Summary,
    Cash,
    Debts,
    Income,
    Bills,
    Savings,
    Living,
    Allocation,
    Plan,
    Facts,
    Forecast
}
