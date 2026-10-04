namespace Cardui.Api.Security;

public sealed class HouseholdScope
{
    public bool IsBound { get; private set; }

    public Guid? HouseholdId { get; private set; }

    /// <summary>
    /// Limits later queries to this household.
    /// </summary>
    public void Bind(Guid householdId)
    {
        IsBound = true;
        HouseholdId = householdId;
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
