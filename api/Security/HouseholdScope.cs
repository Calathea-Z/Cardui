using Cardui.Api.Domain;

namespace Cardui.Api.Security;

public sealed class HouseholdScope
{
    public bool IsBound { get; private set; }

    public Guid? HouseholdId { get; private set; }

    public string PlanningCurrency { get; private set; } = PlanningCurrencyRules.DefaultCode;

    public string TimeZoneId { get; private set; } = HouseholdTime.DefaultTimeZoneId;

    /// <summary>
    /// Limits later queries to this household.
    /// </summary>
    public void Bind(Guid householdId)
    {
        Bind(
            householdId,
            PlanningCurrencyRules.DefaultCode,
            HouseholdTime.DefaultTimeZoneId);
    }

    /// <summary>
    /// Limits later queries to this household and keeps its planning currency and time zone.
    /// </summary>
    public void Bind(Guid householdId, string planningCurrency, string timeZoneId)
    {
        IsBound = true;
        HouseholdId = householdId;
        PlanningCurrency = planningCurrency;
        TimeZoneId = timeZoneId;
    }

    /// <summary>
    /// Limits later queries to rows that have no household yet.
    /// </summary>
    public void BindUnassigned()
    {
        IsBound = true;
        HouseholdId = null;
    }

    /// <summary>
    /// Throws when the request has not been bound to a household or to unassigned rows.
    /// </summary>
    public void EnsureBound()
    {
        if (!IsBound)
        {
            throw new InvalidOperationException("A household scope is required.");
        }
    }

    /// <summary>
    /// Returns the bound household id. Throws when the scope is missing or unassigned.
    /// </summary>
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
