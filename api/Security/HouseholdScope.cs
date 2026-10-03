namespace Cardui.Api.Security;

public sealed class HouseholdScope
{
    public bool IsBound { get; private set; }

    public Guid? HouseholdId { get; private set; }

    public void Bind(Guid householdId)
    {
        IsBound = true;
        HouseholdId = householdId;
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
