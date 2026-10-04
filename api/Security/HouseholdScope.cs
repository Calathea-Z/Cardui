using Cardui.Api.Domain;

namespace Cardui.Api.Security;

public sealed class HouseholdScope
{
    public bool IsBound { get; private set; }

    public Guid? HouseholdId { get; private set; }

    public string PlanningCurrency { get; private set; } = PlanningCurrencyRules.DefaultCode;

    public string TimeZoneId { get; private set; } = HouseholdTime.DefaultTimeZoneId;

    public void Bind(Guid householdId)
    {
        Bind(
            householdId,
            PlanningCurrencyRules.DefaultCode,
            HouseholdTime.DefaultTimeZoneId);
    }

    public void Bind(Guid householdId, string planningCurrency, string timeZoneId)
    {
        IsBound = true;
        HouseholdId = householdId;
        PlanningCurrency = planningCurrency;
        TimeZoneId = timeZoneId;
    }

    public void BindUnassigned()
    {
        IsBound = true;
        HouseholdId = null;
    }

    public void EnsureBound()
    {
        if (!IsBound)
        {
            throw new InvalidOperationException("A household scope is required.");
        }
    }

    public Guid RequireHouseholdId()
    {
        EnsureBound();

        if (HouseholdId is not Guid householdId)
        {
            throw new InvalidOperationException("A household is required.");
        }

        return householdId;
    }
}
